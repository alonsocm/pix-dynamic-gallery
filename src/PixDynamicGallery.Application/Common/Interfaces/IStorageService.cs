namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the cloud object storage provider (AWS S3, Azure Blob Storage, ...). Keeps
/// every SDK-specific concern out of Application/Domain so swapping providers is a matter of
/// registering a different implementation in Infrastructure's DI composition — no other layer
/// needs to change. See <c>StorageProvider</c> configuration in appsettings.json.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file to the configured bucket/container under <paramref name="objectKey"/> and
    /// returns the URL that guests/the kiosk should use to fetch it.
    /// </summary>
    Task<StorageUploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}

/// <summary>Result of a successful upload: the key persisted for future deletes, and the URL to serve to clients.</summary>
public record StorageUploadResult(string ObjectKey, string Url, long SizeBytes);
