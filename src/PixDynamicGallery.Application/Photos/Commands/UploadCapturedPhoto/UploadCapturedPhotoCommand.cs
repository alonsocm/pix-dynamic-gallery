using MediatR;
using PixDynamicGallery.Application.Photos.Dtos;

namespace PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;

/// <summary>
/// Dispatched by <c>SparkboothWatcherService</c> (Infrastructure) whenever a new capture appears
/// on disk. Registers the photo, streams it to cloud storage, and broadcasts the result over
/// SignalR — this single command is the entire "watcher to guest phone" pipeline.
/// </summary>
public record UploadCapturedPhotoCommand : IRequest<PhotoDto>
{
    public required Guid EventId { get; init; }

    /// <summary>Absolute path of the file Sparkbooth just finished writing.</summary>
    public required string LocalFilePath { get; init; }
}
