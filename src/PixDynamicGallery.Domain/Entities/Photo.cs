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
