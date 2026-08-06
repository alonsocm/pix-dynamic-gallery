using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Photos.Dtos;

namespace PixDynamicGallery.Application.Photos.Queries.GetPhotoById;

/// <summary>Backs the guest landing page (<c>/e/:eventId/p/:photoId</c>) with a single photo's details.</summary>
public record GetPhotoByIdQuery(Guid EventId, Guid PhotoId) : IRequest<PhotoDto>;

public class GetPhotoByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPhotoByIdQuery, PhotoDto>
{
    public async Task<PhotoDto> Handle(GetPhotoByIdQuery request, CancellationToken cancellationToken)
    {
        var photo = await context.Photos
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.EventId == request.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Photo), request.PhotoId);

        return PhotoDto.FromEntity(photo);
    }
}
