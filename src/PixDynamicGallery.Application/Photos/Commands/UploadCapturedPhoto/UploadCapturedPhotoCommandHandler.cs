using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Dtos;
using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;

public class UploadCapturedPhotoCommandHandler(
    IApplicationDbContext context,
    IStorageService storageService,
    ILocalCaptureFileReader fileReader,
    IPhotoStripCropper stripCropper,
    IImageThumbnailGenerator thumbnailGenerator,
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
            await using var localContent = await fileReader.OpenReadAsync(request.LocalFilePath, cancellationToken);

            // Sparkbooth's print composite duplicates the strip twice, side by side, so the physical
            // print can be cut in half — crop it down to a single strip before it ever reaches
            // storage/guests (see IPhotoStripCropper). Best-effort: if cropping doesn't apply or
            // fails, fall back to uploading the original untouched rather than failing the capture.
            Stream? croppedContent = null;
            var contentToUpload = localContent;
            if (stripCropper.CanCrop(photo.ContentType))
            {
                croppedContent = await stripCropper.CropToSingleStripAsync(localContent, photo.ContentType, cancellationToken);
                if (croppedContent is not null)
                {
                    contentToUpload = croppedContent;
                }
                else if (localContent.CanSeek)
                {
                    // The cropper read (and failed to fully decode) the stream — rewind before the
                    // upload below reads it from the top.
                    localContent.Position = 0;
                }
            }

            try
            {
                var objectKey = $"{@event.Slug}/{photo.Id}{Path.GetExtension(photo.FileName)}";
                var result = await storageService.UploadAsync(contentToUpload, objectKey, photo.ContentType, cancellationToken);

                photo.MarkAsUploaded(result.ObjectKey, result.Url, result.SizeBytes);
                await context.SaveChangesAsync(cancellationToken);

                // Generated from the same (already single-strip, if cropped) content that was just
                // uploaded, so the wall thumbnail and the full-size download always show the guest
                // the same photo.
                contentToUpload.Position = 0;
                await GenerateAndAttachThumbnailAsync(@event.Slug, photo, contentToUpload, cancellationToken);
            }
            finally
            {
                if (croppedContent is not null)
                {
                    await croppedContent.DisposeAsync();
                }
            }
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

    /// <summary>
    /// Best-effort: the original already uploaded successfully by the time this runs, so a
    /// thumbnail failure (unsupported/corrupt image, transient storage error, ...) is logged and
    /// swallowed rather than failing the whole capture — the wall simply falls back to
    /// <see cref="Domain.Entities.Photo.Url"/> for this one photo (see <see cref="PhotoDto.ThumbnailUrl"/>).
    /// </summary>
    private async Task GenerateAndAttachThumbnailAsync(
        string eventSlug, Photo photo, Stream originalContent, CancellationToken cancellationToken)
    {
        if (!thumbnailGenerator.CanGenerate(photo.ContentType) || !originalContent.CanSeek)
        {
            return;
        }

        try
        {
            originalContent.Position = 0;
            await using var thumbnailContent = await thumbnailGenerator.GenerateAsync(originalContent, cancellationToken);
            if (thumbnailContent is null)
            {
                return;
            }

            var thumbnailKey = $"{eventSlug}/{photo.Id}-thumb.jpg";
            var thumbnailResult = await storageService.UploadAsync(thumbnailContent, thumbnailKey, "image/jpeg", cancellationToken);

            photo.AttachThumbnail(thumbnailResult.ObjectKey, thumbnailResult.Url);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to generate/upload thumbnail for photo {PhotoId} — the wall will fall back to the full-size original.",
                photo.Id);
        }
    }
}
