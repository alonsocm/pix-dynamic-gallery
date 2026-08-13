using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Events.Dtos;

/// <summary>
/// Admin-only superset of <see cref="EventDto"/> — adds <see cref="WatchFolderPath"/>, which the
/// public <c>GET /api/events/{slug}</c> endpoint deliberately omits (it would leak the booth
/// operator's local filesystem path to anyone with a public event URL). Only ever returned
/// behind <c>[AdminAuth]</c>.
/// </summary>
public record AdminEventDto : EventDto
{
    public required string WatchFolderPath { get; init; }

    public static new AdminEventDto FromEntity(Event @event) => new()
    {
        Id = @event.Id,
        Name = @event.Name,
        Slug = @event.Slug,
        GuestBaseUrl = @event.GuestBaseUrl,
        IsActive = @event.IsActive,
        CreatedAtUtc = @event.CreatedAtUtc,
        PhotoCount = @event.Photos.Count,
        WatchFolderPath = @event.WatchFolderPath,
    };
}
