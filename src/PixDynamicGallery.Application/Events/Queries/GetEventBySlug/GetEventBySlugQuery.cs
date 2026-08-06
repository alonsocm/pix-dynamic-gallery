using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Events.Dtos;

namespace PixDynamicGallery.Application.Events.Queries.GetEventBySlug;

/// <summary>Resolves the kiosk/guest-facing slug (from the URL) to full event details.</summary>
public record GetEventBySlugQuery(string Slug) : IRequest<EventDto>;

public class GetEventBySlugQueryHandler(IApplicationDbContext context) : IRequestHandler<GetEventBySlugQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        var @event = await context.Events
            .Include(e => e.Photos)
            .FirstOrDefaultAsync(e => e.Slug == slug, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Event), slug);

        return EventDto.FromEntity(@event);
    }
}
