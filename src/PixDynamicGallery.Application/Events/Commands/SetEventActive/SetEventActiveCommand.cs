using MediatR;
using PixDynamicGallery.Application.Events.Dtos;

namespace PixDynamicGallery.Application.Events.Commands.SetEventActive;

/// <summary>
/// Admin-only: flips Event.IsActive. SparkboothWatcherService's polling loop
/// (Watcher:RefreshIntervalSeconds) picks up the change and starts/stops watching the folder
/// accordingly, without any extra wiring here.
/// </summary>
public record SetEventActiveCommand : IRequest<EventDto>
{
    public required Guid EventId { get; init; }

    public required bool IsActive { get; init; }
}
