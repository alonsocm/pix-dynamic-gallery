using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance;

/// <summary>
/// Shared math for the paper/ink inventory: the suggested cost/photo (used by
/// <see cref="Commands.AddPhotoExpense"/> and the finance summaries) and remaining stock. Kept out
/// of the entities since it reads across two DbSets (<c>PaperPurchases</c> and
/// <c>EventTransactions</c>) rather than being a single aggregate's invariant.
/// </summary>
public static class PaperStockCalculator
{
    /// <summary>Canon Selphy RP-108 kit fallback (108 sheets, ~$800 MXN) — used only until the studio logs its first real purchase.</summary>
    public const int FallbackSheetsPerKit = 108;

    public const decimal FallbackKitPrice = 800m;

    public static async Task<decimal> GetSuggestedCostPerPhotoAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var latest = await context.PaperPurchases
            .OrderByDescending(p => p.PurchaseDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? FallbackKitPrice / FallbackSheetsPerKit : latest.CostPerSheet;
    }

    public static async Task<int> GetTotalConsumedSheetsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.EventTransactions
            .Where(t => t.Category == FinanceCategory.Photos && t.PhotoCount != null)
            .SumAsync(t => t.PhotoCount!.Value, cancellationToken);
    }
}
