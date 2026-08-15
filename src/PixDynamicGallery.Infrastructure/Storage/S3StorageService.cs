using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Infrastructure.Storage;

/// <summary>AWS S3 implementation of <see cref="IStorageService"/>, backed by the AWS SDK v3 client.</summary>
public class S3StorageService(
    IAmazonS3 s3Client,
    IHttpClientFactory httpClientFactory,
    IOptions<StorageOptions> options,
    ILogger<S3StorageService> logger)
    : IStorageService
{
    private readonly AwsS3Options _options = options.Value.AwsS3;
    private readonly bool _publicRead = options.Value.PublicRead;

    public async Task<StorageUploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("Storage:AwsS3:BucketName is not configured.");
        }

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            CannedACL = _publicRead ? S3CannedACL.PublicRead : S3CannedACL.Private,
            AutoCloseStream = false,
            // The SDK defaults to streaming SigV4 (aws-chunked, "STREAMING-AWS4-HMAC-SHA256-PAYLOAD").
            // Real AWS S3 and MinIO both support that, but Cloudflare R2 doesn't — uploads fail with
            // "STREAMING-AWS4-HMAC-SHA256-PAYLOAD not implemented" (confirmed on a real R2 upload).
            // Disabling it falls back to signing the whole payload up front instead of streaming it in
            // signed chunks — the SDK buffers/hashes the full body before sending, which is irrelevant
            // here since photos are a few MB, not multi-GB streams. Works identically against AWS S3
            // and MinIO too, so this is unconditional, not R2-only.
            UseChunkEncoding = false,
        };

        var response = await s3Client.PutObjectAsync(request, cancellationToken);
        var sizeBytes = content.CanSeek ? content.Length : 0;
        var url = BuildPublicUrl(objectKey);

        logger.LogDebug(
            "Uploaded '{Key}' to S3 bucket '{Bucket}' ({StatusCode})",
            objectKey, _options.BucketName, response.HttpStatusCode);

        await WarmUpPublicUrlAsync(url, cancellationToken);

        return new StorageUploadResult(objectKey, url, sizeBytes);
    }

    /// <summary>
    /// Fetches the object's public URL right after upload, before the caller announces the photo
    /// to guests/the kiosk. Confirmed against a real Cloudflare R2 custom domain (<c>img.somospix.com</c>
    /// fronting this bucket): a brand-new object can 503/504 for several seconds after the
    /// `PutObject` response already succeeded — R2 propagating the write to the edge lags behind the
    /// write confirmation. Without this, that window landed on the guest instead: a guest tapping
    /// "Descargar" within seconds of the photo appearing (their fetch()-based download, which — unlike
    /// a plain navigation — has no browser retry of its own) could hit that gap and see the download
    /// fail. Doing the wait here instead hides it inside the upload pipeline, where it overlaps with
    /// time the guest already spends walking to their phone/scanning the QR.
    /// <para>
    /// Deliberately best-effort: failures are logged, not thrown — a storage hiccup here shouldn't
    /// fail the whole upload (and thus lose the photo) when the object did upload successfully. The
    /// download button's own client-side retry (see the frontend) remains the last line of defense
    /// for whatever this doesn't catch.
    /// </para>
    /// </summary>
    private async Task WarmUpPublicUrlAsync(string url, CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;
        var delay = TimeSpan.FromMilliseconds(500);
        var client = httpClientFactory.CreateClient(nameof(S3StorageService));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogDebug("Warmed up public URL '{Url}' on attempt {Attempt}", url, attempt);
                    return;
                }

                logger.LogDebug(
                    "Warm-up GET for '{Url}' returned {StatusCode} on attempt {Attempt}/{MaxAttempts}",
                    url, (int)response.StatusCode, attempt, maxAttempts);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Warm-up GET for '{Url}' failed on attempt {Attempt}/{MaxAttempts}", url, attempt, maxAttempts);
            }

            if (attempt == maxAttempts)
            {
                logger.LogWarning(
                    "Public URL '{Url}' still not reachable after {MaxAttempts} warm-up attempts — announcing the photo anyway.",
                    url, maxAttempts);
                return;
            }

            await Task.Delay(delay, cancellationToken);
            delay *= 2;
        }
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) =>
        s3Client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);

    private string BuildPublicUrl(string objectKey)
    {
        // PublicUrlBase: no bucket segment in the path at all — e.g. Cloudflare R2's r2.dev
        // subdomain or a custom domain mapped straight to the bucket.
        if (!string.IsNullOrWhiteSpace(_options.PublicUrlBase))
        {
            return $"{_options.PublicUrlBase.TrimEnd('/')}/{objectKey}";
        }

        // PublicServiceUrl (browser-reachable) takes priority over ServiceUrl (container-internal,
        // used by the SDK client) — they differ whenever the API and the browser reach the storage
        // service through different hostnames, e.g. local docker-compose with MinIO.
        var publicBaseUrl = string.IsNullOrWhiteSpace(_options.PublicServiceUrl)
            ? _options.ServiceUrl
            : _options.PublicServiceUrl;

        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            // Path-style URL, used for local/S3-compatible endpoints such as MinIO.
            return $"{publicBaseUrl.TrimEnd('/')}/{_options.BucketName}/{objectKey}";
        }

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{objectKey}";
    }
}
