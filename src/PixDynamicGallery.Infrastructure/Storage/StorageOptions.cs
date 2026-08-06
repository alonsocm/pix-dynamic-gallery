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
    /// hostnames, e.g. <c>http://localhost:9000</c> in local docker-compose.
    /// </summary>
    public string? PublicServiceUrl { get; set; }
}

public class AzureBlobOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "pix-photos";
}
