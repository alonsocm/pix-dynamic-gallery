using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Photos.Dtos;

/// <summary>
/// Wire representation of a <see cref="Photo"/>. Deliberately omits <see cref="Photo.LocalFilePath"/>
/// and <see cref="Photo.StorageKey"/> — those are server-internal and never sent to clients.
/// </summary>
public record PhotoDto
{
    public required Guid Id { get; init; }

    public required Guid EventId { get; init; }

    public required string FileName { get; init; }

    public string? Url { get; init; }

    public required string ContentType { get; init; }

    public long SizeBytes { get; init; }

    public required PhotoStatus Status { get; init; }

    public required DateTimeOffset CapturedAtUtc { get; init; }

    public DateTimeOffset? UploadedAtUtc { get; init; }

    public static PhotoDto FromEntity(Photo photo) => new()
    {
        Id = photo.Id,
        EventId = photo.EventId,
        FileName = photo.FileName,
        Url = photo.Url,
        ContentType = photo.ContentType,
        SizeBytes = photo.SizeBytes,
        Status = photo.Status,
        CapturedAtUtc = photo.CreatedAtUtc,
        UploadedAtUtc = photo.UploadedAtUtc,
    };
}
