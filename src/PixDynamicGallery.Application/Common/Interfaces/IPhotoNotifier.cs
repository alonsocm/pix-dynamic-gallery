using PixDynamicGallery.Application.Photos.Dtos;

namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the real-time transport used to push updates to connected clients (the kiosk
/// screen and guest live-wall PWA). Implemented with a SignalR <c>IHubContext&lt;EventHub&gt;</c>
/// in the API layer, so Application never takes a dependency on SignalR itself.
/// </summary>
public interface IPhotoNotifier
{
    /// <summary>Broadcasts a newly-uploaded photo to every client subscribed to <paramref name="eventId"/>'s group.</summary>
    Task NotifyPhotoUploadedAsync(Guid eventId, PhotoDto photo, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts that a previously failed/pending photo transitioned to failed, so the kiosk can surface it.</summary>
    Task NotifyPhotoFailedAsync(Guid eventId, Guid photoId, string reason, CancellationToken cancellationToken = default);
}
