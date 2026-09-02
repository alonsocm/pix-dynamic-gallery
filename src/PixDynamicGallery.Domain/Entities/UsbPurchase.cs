using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A purchase of USB drives — the studio hands one to the client at every event. Purchases form a
/// running inventory ledger, mirroring <see cref="PaperPurchase"/>: total units bought minus units
/// consumed by <see cref="EventTransaction.CreateUsbExpense"/> rows across all events gives the
/// remaining stock, and the most recent purchase's <see cref="CostPerUnit"/> is the suggested
/// cost/USB for the next auto-calculated USB expense.
/// </summary>
public class UsbPurchase : BaseEntity
{
    public DateTimeOffset PurchaseDate { get; private set; }

    public int UnitsCount { get; private set; }

    public decimal TotalCost { get; private set; }

    public string? Notes { get; private set; }

    public decimal CostPerUnit => UnitsCount == 0 ? 0 : TotalCost / UnitsCount;

    private UsbPurchase()
    {
        // Required by EF Core.
    }

    private UsbPurchase(DateTimeOffset purchaseDate, int unitsCount, decimal totalCost, string? notes)
    {
        PurchaseDate = purchaseDate;
        UnitsCount = unitsCount;
        TotalCost = totalCost;
        Notes = notes;
    }

    public static UsbPurchase Create(DateTimeOffset purchaseDate, int unitsCount, decimal totalCost, string? notes)
    {
        if (unitsCount <= 0)
        {
            throw new DomainException("Units count must be greater than zero.");
        }

        if (totalCost < 0)
        {
            throw new DomainException("Total cost cannot be negative.");
        }

        return new UsbPurchase(purchaseDate, unitsCount, totalCost, notes?.Trim());
    }
}
