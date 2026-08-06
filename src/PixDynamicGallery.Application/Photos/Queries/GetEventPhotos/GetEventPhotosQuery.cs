using MediatR;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Common.Models;
using PixDynamicGallery.Application.Photos.Dtos;
using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Photos.Queries.GetEventPhotos;

/// <summary>Feeds the guest live wall: successfully uploaded photos for an event, newest first, paginated.</summary>
public record GetEventPhotosQuery : IRequest<PaginatedList<PhotoDto>>
{
    public required Guid EventId { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 30;
}

public class GetEventPhotosQueryHandler(IApplicationDbContext context) : IRequestHandler<GetEventPhotosQuery, PaginatedList<PhotoDto>>
{
    public async Task<PaginatedList<PhotoDto>> Handle(GetEventPhotosQuery request, CancellationToken cancellationToken)
    {
        var query = context.Photos
            .Where(p => p.EventId == request.EventId && p.Status == PhotoStatus.Uploaded)
            .OrderByDescending(p => p.UploadedAtUtc);

        // Paginate the entity query (server-side, translatable), then map to DTOs in memory —
        // PhotoDto.FromEntity is a plain C# method and EF Core cannot translate it into SQL.
        var page = await PaginatedList<Photo>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = page.Items.Select(PhotoDto.FromEntity).ToList();
        return new PaginatedList<PhotoDto>(dtos, page.TotalCount, page.PageNumber, request.PageSize);
    }
}
