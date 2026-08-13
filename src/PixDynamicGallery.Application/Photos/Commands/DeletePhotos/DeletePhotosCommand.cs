using MediatR;

namespace PixDynamicGallery.Application.Photos.Commands.DeletePhotos;

/// <summary>Admin-only bulk hard-delete: removes both the cloud object and the DB row for each requested photo.</summary>
public record DeletePhotosCommand : IRequest<DeletePhotosResult>
{
    public required Guid EventId { get; init; }

    public required List<Guid> PhotoIds { get; init; }
}

public record DeletePhotosResult
{
    public required int DeletedCount { get; init; }

    public required List<Guid> NotFoundPhotoIds { get; init; }
}
