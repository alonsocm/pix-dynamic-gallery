using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Agenda.Commands.LinkAgendaEntryToEvent;

public class LinkAgendaEntryToEventCommandHandler(IApplicationDbContext context)
    : IRequestHandler<LinkAgendaEntryToEventCommand, AgendaEntryDto>
{
    public async Task<AgendaEntryDto> Handle(LinkAgendaEntryToEventCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.AgendaEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.AgendaEntryId);

        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        entry.LinkToEvent(request.EventId);
        await context.SaveChangesAsync(cancellationToken);

        return AgendaEntryDto.FromEntity(entry);
    }
}
