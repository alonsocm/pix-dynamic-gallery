namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>USB drive inventory: purchases minus units consumed by Usb-category event expenses.</summary>
public record UsbStockDto
{
    public required List<UsbPurchaseDto> Purchases { get; init; }

    public required int TotalPurchasedUnits { get; init; }

    public required int TotalConsumedUnits { get; init; }

    public required int RemainingUnits { get; init; }

    /// <summary>Most recent purchase's cost/unit, or 0 if no purchase has been logged yet.</summary>
    public required decimal SuggestedCostPerUsb { get; init; }
}
