using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Events.Dtos;

public record EventDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    /// <summary>
    /// Public base URL of the guest-facing PWA (e.g. <c>https://gallery.mystudio.com</c>). Safe to
    /// expose — it's the same value the kiosk needs to build the exact QR target URL client-side,
    /// mirroring <see cref="Event.BuildGuestPhotoUrl"/>/<see cref="Event.BuildGuestWallUrl"/>.
    /// </summary>
    public required string GuestBaseUrl { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public int PhotoCount { get; init; }

    public static EventDto FromEntity(Event @event) => new()
    {
        Id = @event.Id,
        Name = @event.Name,
        Slug = @event.Slug,
        GuestBaseUrl = @event.GuestBaseUrl,
        IsActive = @event.IsActive,
        CreatedAtUtc = @event.CreatedAtUtc,
        PhotoCount = @event.Photos.Count,
    };
}
