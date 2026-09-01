using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Photos.Commands.DeletePhotos;

public class DeletePhotosCommandHandler(
    IApplicationDbContext context,
    IStorageService storageService,
    ILogger<DeletePhotosCommandHandler> logger)
    : IRequestHandler<DeletePhotosCommand, DeletePhotosResult>
{
    public async Task<DeletePhotosResult> Handle(DeletePhotosCommand request, CancellationToken cancellationToken)
    {
        var photos = await context.Photos
            .Where(p => p.EventId == request.EventId && request.PhotoIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var foundIds = photos.Select(p => p.Id).ToHashSet();
        var notFoundIds = request.PhotoIds.Where(id => !foundIds.Contains(id)).ToList();

        foreach (var photo in photos)
        {
            await DeleteStorageObjectAsync(photo.Id, photo.StorageKey, cancellationToken);
            await DeleteStorageObjectAsync(photo.Id, photo.ThumbnailStorageKey, cancellationToken);
        }

        context.Photos.RemoveRange(photos);
        await context.SaveChangesAsync(cancellationToken);

        return new DeletePhotosResult { DeletedCount = photos.Count, NotFoundPhotoIds = notFoundIds };
    }

    /// <summary>
    /// Best-effort: a bulk admin delete shouldn't get stuck on one flaky storage call — this leaves
    /// a harmless orphaned object behind (solo-operator tool, not a billing-critical system) while
    /// still letting the operator clear the row. Shared by both the original and the thumbnail key.
    /// </summary>
    private async Task DeleteStorageObjectAsync(Guid photoId, string? storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(storageKey))
        {
            return; // never finished uploading (or no thumbnail was generated) — nothing to clean up
        }

        try
        {
            await storageService.DeleteAsync(storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to delete storage object {StorageKey} for photo {PhotoId} — deleting the DB row anyway.",
                storageKey, photoId);
        }
    }
}
