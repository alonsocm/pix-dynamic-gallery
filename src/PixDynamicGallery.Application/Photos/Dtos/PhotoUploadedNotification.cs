namespace PixDynamicGallery.Application.Photos.Dtos;

/// <summary>
/// Payload broadcast over SignalR as <c>OnPhotoUploaded</c> to every client in an event's group
/// (kiosk + guest live wall). Intentionally slim — clients that need more detail fetch
/// <c>GET /api/events/{eventId}/photos/{photoId}</c>.
/// </summary>
public record PhotoUploadedNotification
{
    public required Guid PhotoId { get; init; }

    public required Guid EventId { get; init; }

    public required string Url { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public static PhotoUploadedNotification FromDto(PhotoDto photo) => new()
    {
        PhotoId = photo.Id,
        EventId = photo.EventId,
        Url = photo.Url ?? throw new InvalidOperationException($"Photo {photo.Id} has no URL yet; cannot notify."),
        Timestamp = photo.UploadedAtUtc ?? DateTimeOffset.UtcNow,
    };
}
