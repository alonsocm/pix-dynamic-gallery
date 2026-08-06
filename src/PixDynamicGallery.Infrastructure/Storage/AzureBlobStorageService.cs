using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Infrastructure.Storage;

/// <summary>Azure Blob Storage implementation of <see cref="IStorageService"/>.</summary>
public class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<StorageOptions> options,
    ILogger<AzureBlobStorageService> logger)
    : IStorageService
{
    private readonly AzureBlobOptions _options = options.Value.AzureBlob;
    private readonly bool _publicRead = options.Value.PublicRead;

    public async Task<StorageUploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync(
            _publicRead ? PublicAccessType.Blob : PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(objectKey);

        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

        logger.LogDebug(
            "Uploaded '{Key}' to Azure container '{Container}'",
            objectKey, _options.ContainerName);

        return new StorageUploadResult(objectKey, blob.Uri.ToString(), properties.Value.ContentLength);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await container.DeleteBlobIfExistsAsync(objectKey, cancellationToken: cancellationToken);
    }
}
