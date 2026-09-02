using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Agenda.Queries.GetAgendaEntries;

/// <summary>Admin-only: every booking, optionally filtered by date range and/or status — powers the /admin/agenda screen.</summary>
public record GetAgendaEntriesQuery : IRequest<List<AgendaEntryDto>>
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public AgendaStatus? Status { get; init; }
}

public class GetAgendaEntriesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetAgendaEntriesQuery, List<AgendaEntryDto>>
{
    public async Task<List<AgendaEntryDto>> Handle(GetAgendaEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = context.AgendaEntries.AsQueryable();

        if (request.From.HasValue)
        {
            query = query.Where(a => a.EventDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.EventDate <= request.To.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        var entries = await query
            .OrderBy(a => a.EventDate)
            .ToListAsync(cancellationToken);

        var entryIds = entries.Select(e => e.Id).ToList();
        var depositsByEntry = (await context.AgendaDeposits
                .Where(d => entryIds.Contains(d.AgendaEntryId))
                .ToListAsync(cancellationToken))
            .GroupBy(d => d.AgendaEntryId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Domain.Entities.AgendaDeposit>)g.OrderByDescending(d => d.PaymentDate).ToList());

        return entries
            .Select(e => AgendaEntryDto.FromEntity(e, depositsByEntry.GetValueOrDefault(e.Id, [])))
            .ToList();
    }
}
