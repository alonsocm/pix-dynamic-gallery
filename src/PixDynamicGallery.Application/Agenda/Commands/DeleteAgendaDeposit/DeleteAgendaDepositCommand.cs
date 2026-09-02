using MediatR;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaDeposit;

public record DeleteAgendaDepositCommand : IRequest
{
    public required Guid Id { get; init; }
}
