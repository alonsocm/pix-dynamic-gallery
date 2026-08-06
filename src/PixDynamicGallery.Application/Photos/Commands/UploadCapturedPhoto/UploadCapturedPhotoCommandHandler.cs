using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Dtos;

namespace PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;

public class UploadCapturedPhotoCommandHandler(
    IApplicationDbContext context,
    IStorageService storageService,
    ILocalCaptureFileReader fileReader,
    IPhotoNotifier notifier,
    ILogger<UploadCapturedPhotoCommandHandler> logger)
    : IRequestHandler<UploadCapturedPhotoCommand, PhotoDto>
{
    public async Task<PhotoDto> Handle(UploadCapturedPhotoCommand request, CancellationToken cancellationToken)
    {
        var @event = await context.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);

        var fileName = Path.GetFileName(request.LocalFilePath);
        var photo = @event.RegisterCapturedPhoto(fileName, request.LocalFilePath);

        // Explicitly Add() rather than relying on graph fixup from the (unloaded) Event.Photos
        // navigation: Photo.Id is set client-side (Guid.NewGuid() in BaseEntity) before this
        // entity is ever tracked, so without an explicit Add(), EF Core's change tracker has no
        // reliable way to tell "brand new row" apart from "existing row, unmodified" and ends up
        // generating an UPDATE instead of an INSERT.
        context.Photos.Add(photo);

        // Persist the "Pending" row first so the photo has a stable Id (used as the object key)
        // even if the upload below fails — failures stay visible/queryable instead of vanishing.
        await context.SaveChangesAsync(cancellationToken);

        photo.MarkAsUploading();
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await using var content = await fileReader.OpenReadAsync(request.LocalFilePath, cancellationToken);

            var objectKey = $"{@event.Slug}/{photo.Id}{Path.GetExtension(fileName)}";
            var result = await storageService.UploadAsync(content, objectKey, photo.ContentType, cancellationToken);

            photo.MarkAsUploaded(result.ObjectKey, result.Url, result.SizeBytes);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload captured photo {PhotoId} ({FilePath}) for event {EventId}",
                photo.Id, request.LocalFilePath, request.EventId);

            photo.MarkAsFailed(ex.Message);
            await context.SaveChangesAsync(cancellationToken);

            await notifier.NotifyPhotoFailedAsync(@event.Id, photo.Id, ex.Message, cancellationToken);
            throw;
        }

        var dto = PhotoDto.FromEntity(photo);
        await notifier.NotifyPhotoUploadedAsync(@event.Id, dto, cancellationToken);

        logger.LogInformation("Photo {PhotoId} uploaded for event {EventId} ({Slug}): {Url}",
            photo.Id, @event.Id, @event.Slug, photo.Url);

        return dto;
    }
}
