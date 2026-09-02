using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;

namespace PixDynamicGallery.Application.Agenda.Commands.AddAgendaDeposit;

/// <summary>Logs a deposit/advance payment against a booking, before there's a technical Event to record it as an EventTransaction.</summary>
public record AddAgendaDepositCommand : IRequest<AgendaDepositDto>
{
    public required Guid AgendaEntryId { get; init; }

    public required decimal Amount { get; init; }

    public required DateTimeOffset PaymentDate { get; init; }

    public string? Notes { get; init; }
}
