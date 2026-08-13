using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Infrastructure.Storage;

/// <summary>AWS S3 implementation of <see cref="IStorageService"/>, backed by the AWS SDK v3 client.</summary>
public class S3StorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options, ILogger<S3StorageService> logger)
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

        return new StorageUploadResult(objectKey, url, sizeBytes);
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
