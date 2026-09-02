using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Agenda.Commands.SetAgendaStatus;

public record SetAgendaStatusCommand : IRequest<AgendaEntryDto>
{
    public required Guid Id { get; init; }

    public required AgendaStatus Status { get; init; }
}
