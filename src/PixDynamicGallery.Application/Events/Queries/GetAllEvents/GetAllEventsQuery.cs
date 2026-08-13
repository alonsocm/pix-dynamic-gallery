using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Events.Dtos;

namespace PixDynamicGallery.Application.Events.Queries.GetAllEvents;

/// <summary>Admin-only: every event, newest first — powers the /admin/events screen. Not paginated (dozens of events at most).</summary>
public record GetAllEventsQuery : IRequest<List<AdminEventDto>>;

public class GetAllEventsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAllEventsQuery, List<AdminEventDto>>
{
    public async Task<List<AdminEventDto>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await context.Events
            .Include(e => e.Photos)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return events.Select(AdminEventDto.FromEntity).ToList();
    }
}
