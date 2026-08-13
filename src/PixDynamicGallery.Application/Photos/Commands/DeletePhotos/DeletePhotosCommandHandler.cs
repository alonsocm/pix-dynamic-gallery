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
            if (string.IsNullOrEmpty(photo.StorageKey))
            {
                continue; // never finished uploading — nothing in storage to clean up
            }

            try
            {
                await storageService.DeleteAsync(photo.StorageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort: a bulk admin delete shouldn't get stuck on one flaky storage call —
                // this leaves a harmless orphaned object behind (solo-operator tool, not a
                // billing-critical system) while still letting the operator clear the row.
                logger.LogWarning(ex,
                    "Failed to delete storage object {StorageKey} for photo {PhotoId} — deleting the DB row anyway.",
                    photo.StorageKey, photo.Id);
            }
        }

        context.Photos.RemoveRange(photos);
        await context.SaveChangesAsync(cancellationToken);

        return new DeletePhotosResult { DeletedCount = photos.Count, NotFoundPhotoIds = notFoundIds };
    }
}
