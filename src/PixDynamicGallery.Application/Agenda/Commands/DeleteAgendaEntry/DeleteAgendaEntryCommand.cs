using MediatR;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaEntry;

public record DeleteAgendaEntryCommand : IRequest
{
    public required Guid Id { get; init; }
}
