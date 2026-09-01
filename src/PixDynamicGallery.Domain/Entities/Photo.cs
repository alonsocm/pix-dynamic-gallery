using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Enums;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A single capture (photo or GIF) produced by Sparkbooth for an <see cref="Entities.Event"/>.
/// Tracks the file from local disk detection through cloud upload.
/// </summary>
public class Photo : BaseEntity
{
    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    /// <summary>Original file name written by Sparkbooth, e.g. <c>IMG_20260805_193045.jpg</c>.</summary>
    public string FileName { get; private set; } = default!;

    /// <summary>Absolute path on the kiosk machine at the moment it was detected. Not exposed to guests.</summary>
    public string LocalFilePath { get; private set; } = default!;

    /// <summary>Object key/path inside the cloud storage bucket/container.</summary>
    public string? StorageKey { get; private set; }

    /// <summary>Public (or pre-signed) URL guests and the kiosk use to display/download the photo.</summary>
    public string? Url { get; private set; }

    /// <summary>Object key of the resized preview JPEG (see <see cref="Url"/>'s counterpart below). Null until <see cref="AttachThumbnail"/> runs.</summary>
    public string? ThumbnailStorageKey { get; private set; }

    /// <summary>
    /// URL of a small static JPEG preview, generated best-effort right after upload. The guest live
    /// wall renders this instead of <see cref="Url"/> so tiles don't each pull down the full-size
    /// original (or, for animated GIFs, the whole animation) — null whenever generation failed or
    /// hasn't run yet, in which case callers fall back to <see cref="Url"/>.
    /// </summary>
    public string? ThumbnailUrl { get; private set; }

    public string ContentType { get; private set; } = "image/jpeg";

    public long SizeBytes { get; private set; }

    public PhotoStatus Status { get; private set; } = PhotoStatus.Pending;

    public DateTimeOffset? UploadedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    private Photo()
    {
        // Required by EF Core.
    }

    private Photo(Guid eventId, string fileName, string localFilePath)
    {
        EventId = eventId;
        FileName = fileName;
        LocalFilePath = localFilePath;
        ContentType = InferContentType(fileName);
    }

    public static Photo Create(Guid eventId, string fileName, string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("Photo file name is required.");
        }

        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new DomainException("Photo local file path is required.");
        }

        return new Photo(eventId, fileName, localFilePath);
    }

    public void MarkAsUploading()
    {
        if (Status is PhotoStatus.Uploaded)
        {
            throw new DomainException($"Photo '{Id}' was already uploaded; cannot re-upload.");
        }

        Status = PhotoStatus.Uploading;
    }

    public void MarkAsUploaded(string storageKey, string url, long sizeBytes)
    {
        StorageKey = storageKey;
        Url = url;
        SizeBytes = sizeBytes;
        Status = PhotoStatus.Uploaded;
        UploadedAtUtc = DateTimeOffset.UtcNow;
        FailureReason = null;
    }

    /// <summary>
    /// Records a successfully generated/uploaded thumbnail. Separate from <see cref="MarkAsUploaded"/>
    /// because thumbnail generation is a best-effort step that runs after (and must not block or
    /// undo) the original upload succeeding — a photo can legitimately stay <see cref="PhotoStatus.Uploaded"/>
    /// with no thumbnail if generation failed.
    /// </summary>
    public void AttachThumbnail(string storageKey, string url)
    {
        ThumbnailStorageKey = storageKey;
        ThumbnailUrl = url;
    }

    public void MarkAsFailed(string reason)
    {
        Status = PhotoStatus.Failed;
        FailureReason = reason;
    }

    private static string InferContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream",
    };
}
