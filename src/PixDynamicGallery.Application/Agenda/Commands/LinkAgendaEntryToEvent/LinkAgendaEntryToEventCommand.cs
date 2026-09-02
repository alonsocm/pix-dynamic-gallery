using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;

namespace PixDynamicGallery.Application.Agenda.Commands.LinkAgendaEntryToEvent;

/// <summary>Records which technical Event a booking turned into, once the studio creates it.</summary>
public record LinkAgendaEntryToEventCommand : IRequest<AgendaEntryDto>
{
    public required Guid AgendaEntryId { get; init; }

    public required Guid EventId { get; init; }
}
