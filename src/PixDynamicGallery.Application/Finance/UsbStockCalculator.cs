using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance;

/// <summary>
/// Shared math for the USB drive inventory: the suggested cost/USB (used by
/// <see cref="Commands.AddUsbExpense"/> and the finance summaries) and remaining stock. Mirrors
/// <see cref="PaperStockCalculator"/> — see its doc comment for why this reads across two DbSets
/// instead of living on an entity.
/// </summary>
public static class UsbStockCalculator
{
    public static async Task<decimal> GetSuggestedCostPerUsbAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var latest = await context.UsbPurchases
            .OrderByDescending(p => p.PurchaseDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return latest?.CostPerUnit ?? 0m;
    }

    public static async Task<int> GetTotalConsumedUnitsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.EventTransactions
            .Where(t => t.Category == FinanceCategory.Usb && t.UsbCount != null)
            .SumAsync(t => t.UsbCount!.Value, cancellationToken);
    }
}
