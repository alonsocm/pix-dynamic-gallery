using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A deposit/advance payment a client made to reserve a date, logged against an
/// <see cref="AgendaEntry"/> before any technical <see cref="Event"/> exists to attach a
/// <see cref="EventTransaction"/> to. Once the studio links the booking to an <see cref="Event"/>
/// (<see cref="AgendaEntry.LinkToEvent"/>), every deposit not yet transferred is converted into a
/// <see cref="FinanceCategory.Payment"/> income <see cref="EventTransaction"/> on that event — see
/// <c>LinkAgendaEntryToEventCommandHandler</c> — and <see cref="MarkTransferred"/> records that so
/// it's never converted twice.
/// </summary>
public class AgendaDeposit : BaseEntity
{
    public Guid AgendaEntryId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset PaymentDate { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Set once this deposit has been converted into an <see cref="EventTransaction"/> — null until the booking is linked to an Event.</summary>
    public Guid? TransferredTransactionId { get; private set; }

    private AgendaDeposit()
    {
        // Required by EF Core.
    }

    private AgendaDeposit(Guid agendaEntryId, decimal amount, DateTimeOffset paymentDate, string? notes)
    {
        AgendaEntryId = agendaEntryId;
        Amount = amount;
        PaymentDate = paymentDate;
        Notes = notes;
    }

    public static AgendaDeposit Create(Guid agendaEntryId, decimal amount, DateTimeOffset paymentDate, string? notes)
    {
        if (amount <= 0)
        {
            throw new DomainException("Deposit amount must be greater than zero.");
        }

        return new AgendaDeposit(agendaEntryId, amount, paymentDate, notes?.Trim());
    }

    public void MarkTransferred(Guid transactionId) => TransferredTransactionId = transactionId;
}
