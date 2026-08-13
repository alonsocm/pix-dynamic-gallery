using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A single photobooth event (a wedding, a corporate party, etc). Owns the folder that
/// Sparkbooth writes to, the branding shown on the guest landing page, and the collection of
/// photos captured during it.
/// </summary>
public class Event : BaseEntity
{
    private readonly List<Photo> _photos = [];

    public string Name { get; private set; } = default!;

    /// <summary>URL-safe unique identifier used in routes, e.g. <c>/e/{Slug}/wall</c>.</summary>
    public string Slug { get; private set; } = default!;

    /// <summary>Absolute local path Sparkbooth writes finished captures to, e.g. <c>C:\SparkboothPhotos\</c>.</summary>
    public string WatchFolderPath { get; private set; } = default!;

    /// <summary>Public base URL of the guest-facing PWA, used to build the QR code payload.</summary>
    public string GuestBaseUrl { get; private set; } = default!;

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Photo> Photos => _photos.AsReadOnly();

    private Event()
    {
        // Required by EF Core.
    }

    private Event(string name, string slug, string watchFolderPath, string guestBaseUrl)
    {
        Name = name;
        Slug = slug;
        WatchFolderPath = watchFolderPath;
        GuestBaseUrl = guestBaseUrl.TrimEnd('/');
        IsActive = true;
    }

    public static Event Create(string name, string slug, string watchFolderPath, string guestBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event name is required.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Event slug is required.");
        }

        if (string.IsNullOrWhiteSpace(watchFolderPath))
        {
            throw new DomainException("A watch folder path is required so the watcher knows where Sparkbooth writes photos.");
        }

        if (string.IsNullOrWhiteSpace(guestBaseUrl))
        {
            throw new DomainException("A guest base URL is required to build QR codes.");
        }

        return new Event(name.Trim(), slug.Trim().ToLowerInvariant(), watchFolderPath.Trim(), guestBaseUrl.Trim());
    }

    /// <summary>Builds the deep link guests scan from the kiosk QR for a given photo.</summary>
    public string BuildGuestPhotoUrl(Guid photoId) => $"{GuestBaseUrl}/e/{Slug}/p/{photoId}";

    public string BuildGuestWallUrl() => $"{GuestBaseUrl}/e/{Slug}/wall";

    public Photo RegisterCapturedPhoto(string fileName, string localFilePath)
    {
        if (!IsActive)
        {
            throw new DomainException($"Cannot register a photo for inactive event '{Slug}'.");
        }

        var photo = Photo.Create(Id, fileName, localFilePath);
        _photos.Add(photo);
        return photo;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
