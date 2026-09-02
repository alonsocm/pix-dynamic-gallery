using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Agenda.Commands.UpdateAgendaEntry;

public class UpdateAgendaEntryCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateAgendaEntryCommand, AgendaEntryDto>
{
    public async Task<AgendaEntryDto> Handle(UpdateAgendaEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.Id);

        entry.UpdateDetails(
            request.ClientName,
            request.EventType,
            request.EventDate,
            request.ContactPhone,
            request.ContactEmail,
            request.Location,
            request.Notes,
            request.AgreedPrice);

        await context.SaveChangesAsync(cancellationToken);

        return AgendaEntryDto.FromEntity(entry);
    }
}
