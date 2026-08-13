using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Dtos;
using PixDynamicGallery.Domain.Enums;

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

        // Idempotent per (EventId, LocalFilePath): SparkboothWatcherService retries a capture that
        // previously failed (e.g. a connectivity blip while the cabin was offline) by re-dispatching
        // this same command for the same file — reuse whatever row that earlier attempt left behind
        // instead of creating a second Photo for the same capture.
        var photo = await context.Photos.FirstOrDefaultAsync(
            p => p.EventId == request.EventId && p.LocalFilePath == request.LocalFilePath, cancellationToken);

        if (photo is not null && photo.Status == PhotoStatus.Uploaded)
        {
            // Already succeeded on an earlier attempt — nothing to do, and skip re-notifying so
            // guests/kiosk (who already got the first OnPhotoUploaded) don't see it announced twice.
            return PhotoDto.FromEntity(photo);
        }

        if (photo is null)
        {
            var fileName = Path.GetFileName(request.LocalFilePath);
            photo = @event.RegisterCapturedPhoto(fileName, request.LocalFilePath);

            // Explicitly Add() rather than relying on graph fixup from the (unloaded) Event.Photos
            // navigation: Photo.Id is set client-side (Guid.NewGuid() in BaseEntity) before this
            // entity is ever tracked, so without an explicit Add(), EF Core's change tracker has no
            // reliable way to tell "brand new row" apart from "existing row, unmodified" and ends up
            // generating an UPDATE instead of an INSERT.
            context.Photos.Add(photo);

            // Persist the "Pending" row first so the photo has a stable Id (used as the object key)
            // even if the upload below fails — failures stay visible/queryable instead of vanishing.
            await context.SaveChangesAsync(cancellationToken);
        }

        photo.MarkAsUploading();
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await using var content = await fileReader.OpenReadAsync(request.LocalFilePath, cancellationToken);

            var objectKey = $"{@event.Slug}/{photo.Id}{Path.GetExtension(photo.FileName)}";
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
