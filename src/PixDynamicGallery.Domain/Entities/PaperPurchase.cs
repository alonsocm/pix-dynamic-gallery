using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A purchase of photo paper/ink stock (e.g. a Canon Selphy RP-108 kit: 108 sheets for ~$800 MXN,
/// price varies with promotions). Purchases form a running inventory ledger — total sheets bought
/// minus sheets consumed by <see cref="EventTransaction.CreatePhotoExpense"/> rows across all events
/// gives the remaining stock, and the most recent purchase's <see cref="CostPerSheet"/> is the
/// suggested cost/photo for the next auto-calculated photo expense.
/// </summary>
public class PaperPurchase : BaseEntity
{
    public DateTimeOffset PurchaseDate { get; private set; }

    public int SheetsCount { get; private set; }

    public decimal TotalCost { get; private set; }

    public string? Notes { get; private set; }

    public decimal CostPerSheet => SheetsCount == 0 ? 0 : TotalCost / SheetsCount;

    private PaperPurchase()
    {
        // Required by EF Core.
    }

    private PaperPurchase(DateTimeOffset purchaseDate, int sheetsCount, decimal totalCost, string? notes)
    {
        PurchaseDate = purchaseDate;
        SheetsCount = sheetsCount;
        TotalCost = totalCost;
        Notes = notes;
    }

    public static PaperPurchase Create(DateTimeOffset purchaseDate, int sheetsCount, decimal totalCost, string? notes)
    {
        if (sheetsCount <= 0)
        {
            throw new DomainException("Sheets count must be greater than zero.");
        }

        if (totalCost < 0)
        {
            throw new DomainException("Total cost cannot be negative.");
        }

        return new PaperPurchase(purchaseDate, sheetsCount, totalCost, notes?.Trim());
    }
}
