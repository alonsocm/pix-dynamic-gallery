using Microsoft.AspNetCore.SignalR;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Dtos;

namespace PixDynamicGallery.Api.Hubs;

/// <summary>
/// SignalR-backed implementation of <see cref="IPhotoNotifier"/>. Lives in the API layer (rather
/// than Infrastructure) because it is coupled to <see cref="EventHub"/>, which is itself a
/// transport/presentation concern — Application only ever sees the <see cref="IPhotoNotifier"/>
/// abstraction and has no reference to SignalR.
/// </summary>
public class PhotoNotifier(IHubContext<EventHub> hubContext) : IPhotoNotifier
{
    public Task NotifyPhotoUploadedAsync(Guid eventId, PhotoDto photo, CancellationToken cancellationToken = default)
    {
        var notification = PhotoUploadedNotification.FromDto(photo);

        return hubContext.Clients
            .Group(EventHub.GetGroupName(eventId))
            .SendAsync("OnPhotoUploaded", notification, cancellationToken);
    }

    public Task NotifyPhotoFailedAsync(Guid eventId, Guid photoId, string reason, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .Group(EventHub.GetGroupName(eventId))
            .SendAsync("OnPhotoFailed", new { photoId, eventId, reason }, cancellationToken);
    }
}
