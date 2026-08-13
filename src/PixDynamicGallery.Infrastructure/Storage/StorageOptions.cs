namespace PixDynamicGallery.Infrastructure.Storage;

public enum StorageProvider
{
    AwsS3,
    AzureBlob,
}

/// <summary>
/// Bound from the <c>Storage</c> section of configuration. Only the section matching
/// <see cref="Provider"/> needs to be filled in; the other is ignored. See appsettings.json.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; set; } = StorageProvider.AwsS3;

    /// <summary>
    /// If true, uploaded objects are set to public-read so <see cref="StorageUploadResult.Url" />
    /// (from <c>Common.Interfaces.IStorageService</c>) can be served straight to guests — the
    /// simplest option for an MVP. Set to false and front the bucket/container with a CDN or
    /// pre-signed URLs for a stricter production setup.
    /// <para>
    /// On Cloudflare R2, per-object ACLs are accepted but have no effect (R2 doesn't implement S3
    /// ACLs) — public access is instead a bucket-level setting in the R2 dashboard (enable the
    /// <c>r2.dev</c> subdomain, or connect a custom domain). This flag is still honored for
    /// MinIO/vanilla S3, so leave it true and configure the bucket's own public access separately
    /// when <see cref="AwsS3Options.PublicUrlBase"/> is set.
    /// </para>
    /// </summary>
    public bool PublicRead { get; set; } = true;

    public AwsS3Options AwsS3 { get; set; } = new();

    public AzureBlobOptions AzureBlob { get; set; } = new();
}

public class AwsS3Options
{
    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";

    /// <summary>Leave empty to use the default AWS credential chain (recommended: IAM role, env vars, SSO profile).</summary>
    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Endpoint the API container uses to *talk to* the storage service, e.g. the Docker-internal
    /// <c>http://minio:9000</c>. Only set for S3-compatible endpoints (MinIO, etc.) — leave empty
    /// for real AWS S3, where the SDK resolves the endpoint from <see cref="Region"/>.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Endpoint used only to *build the URLs returned to clients* (browsers can't resolve Docker
    /// service names like <c>minio</c>). Defaults to <see cref="ServiceUrl"/> when unset — set this
    /// separately whenever the API and the browser reach the storage service through different
    /// hostnames, e.g. <c>http://localhost:9000</c> in local docker-compose. Combined with
    /// <see cref="BucketName"/> as <c>{PublicServiceUrl}/{BucketName}/{key}</c> (path-style), which
    /// is what MinIO and vanilla S3-compatible endpoints expect. Ignored when
    /// <see cref="PublicUrlBase"/> is set.
    /// </summary>
    public string? PublicServiceUrl { get; set; }

    /// <summary>
    /// Full public base URL prepended directly to the object key, with <em>no</em> bucket path
    /// segment: <c>{PublicUrlBase}/{key}</c>. Needed for providers whose public URL doesn't include
    /// the bucket name — e.g. Cloudflare R2's <c>r2.dev</c> subdomain
    /// (<c>https://pub-xxxx.r2.dev</c>) or a custom domain mapped to the bucket
    /// (<c>https://fotos.somospix.com</c>). Takes priority over <see cref="PublicServiceUrl"/> when
    /// set. Leave unset for MinIO/path-style endpoints.
    /// </summary>
    public string? PublicUrlBase { get; set; }
}

public class AzureBlobOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "pix-photos";
}
