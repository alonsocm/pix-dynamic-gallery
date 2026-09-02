namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>Paper/ink inventory: purchases minus sheets consumed by Photos-category event expenses.</summary>
public record PaperStockDto
{
    public required List<PaperPurchaseDto> Purchases { get; init; }

    public required int TotalPurchasedSheets { get; init; }

    public required int TotalConsumedSheets { get; init; }

    public required int RemainingSheets { get; init; }

    /// <summary>Most recent purchase's cost/sheet, or the Canon RP-108 kit fallback (800/108) if no purchases exist yet.</summary>
    public required decimal SuggestedCostPerPhoto { get; init; }
}
