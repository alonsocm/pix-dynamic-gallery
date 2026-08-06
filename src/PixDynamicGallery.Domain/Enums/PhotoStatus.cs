namespace PixDynamicGallery.Domain.Enums;

/// <summary>
/// Lifecycle of a photo as it moves from "detected on disk by the watcher" to "available to guests".
/// </summary>
public enum PhotoStatus
{
    /// <summary>Detected locally by the watcher, not yet uploaded to cloud storage.</summary>
    Pending = 0,

    /// <summary>Upload to the configured <c>IStorageService</c> is in progress.</summary>
    Uploading = 1,

    /// <summary>Stored in the cloud and safe to broadcast/show to guests.</summary>
    Uploaded = 2,

    /// <summary>Upload failed; see logs. Eligible for retry.</summary>
    Failed = 3,
}
