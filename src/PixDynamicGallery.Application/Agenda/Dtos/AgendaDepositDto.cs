using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Agenda.Dtos;

public record AgendaDepositDto
{
    public required Guid Id { get; init; }

    public required Guid AgendaEntryId { get; init; }

    public required decimal Amount { get; init; }

    public required DateTimeOffset PaymentDate { get; init; }

    public string? Notes { get; init; }

    /// <summary>Non-null once this deposit has been converted into an income transaction on the linked Event.</summary>
    public Guid? TransferredTransactionId { get; init; }

    public static AgendaDepositDto FromEntity(AgendaDeposit deposit) => new()
    {
        Id = deposit.Id,
        AgendaEntryId = deposit.AgendaEntryId,
        Amount = deposit.Amount,
        PaymentDate = deposit.PaymentDate,
        Notes = deposit.Notes,
        TransferredTransactionId = deposit.TransferredTransactionId,
    };
}
