using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Agenda.Commands.CreateAgendaEntry;

public class CreateAgendaEntryCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateAgendaEntryCommand, AgendaEntryDto>
{
    public async Task<AgendaEntryDto> Handle(CreateAgendaEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = AgendaEntry.Create(
            request.ClientName,
            request.EventType,
            request.EventDate,
            request.ContactPhone,
            request.ContactEmail,
            request.Location,
            request.Notes,
            request.AgreedPrice);

        context.AgendaEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return AgendaEntryDto.FromEntity(entry, deposits: []);
    }
}
