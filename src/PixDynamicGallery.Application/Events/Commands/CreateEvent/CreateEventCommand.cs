using MediatR;
using PixDynamicGallery.Application.Events.Dtos;

namespace PixDynamicGallery.Application.Events.Commands.CreateEvent;

/// <summary>Creates a new event and the local folder/Sparkbooth binding the watcher will monitor for it.</summary>
public record CreateEventCommand : IRequest<EventDto>
{
    public required string Name { get; init; }

    public required string Slug { get; init; }

    /// <summary>Absolute local path where Sparkbooth saves this event's captures, e.g. <c>C:\SparkboothPhotos\WeddingJulia\</c>.</summary>
    public required string WatchFolderPath { get; init; }

    /// <summary>Base URL of the deployed guest PWA, e.g. <c>https://gallery.mystudio.com</c>.</summary>
    public required string GuestBaseUrl { get; init; }
}
