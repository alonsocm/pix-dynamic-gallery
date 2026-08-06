using Microsoft.AspNetCore.SignalR;

namespace PixDynamicGallery.Api.Hubs;

/// <summary>
/// Real-time hub for a single event. Clients (the kiosk screen and every guest's browser on the
/// live wall) join a SignalR group named after the event's <c>Id</c> so broadcasts never cross
/// between concurrently running events. The server-to-client contract is a single event,
/// <c>OnPhotoUploaded</c> (see <see cref="Application.Photos.Dtos.PhotoUploadedNotification"/>),
/// plus <c>OnPhotoFailed</c> for surfacing upload failures on the kiosk.
/// </summary>
public class EventHub : Hub
{
    private static string GroupName(Guid eventId) => $"event-{eventId}";

    public async Task JoinEventGroup(Guid eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public async Task LeaveEventGroup(Guid eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    /// <summary>Exposed so server-side notifiers (<see cref="PhotoNotifier"/>) resolve the same group name as clients.</summary>
    public static string GetGroupName(Guid eventId) => GroupName(eventId);
}
