namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>Transactions for one event plus their totals — powers /admin/events/:eventId/finance.</summary>
public record EventFinanceSummaryDto
{
    public required Guid EventId { get; init; }

    public required List<EventTransactionDto> Transactions { get; init; }

    public required decimal TotalIncome { get; init; }

    public required decimal TotalExpense { get; init; }

    public required decimal Profit { get; init; }

    /// <summary>Suggested photo count for the next auto-calculated Photos expense — uploaded photos for this event.</summary>
    public required int SuggestedPhotoCount { get; init; }

    /// <summary>Suggested cost/photo — the most recent paper purchase's cost/sheet, or the kit fallback if none exist.</summary>
    public required decimal SuggestedCostPerPhoto { get; init; }

    public required decimal SuggestedCostPerKm { get; init; }

    /// <summary>Suggested USB count for the next auto-calculated Usb expense — normally 1 (one drive handed out per event).</summary>
    public required int SuggestedUsbCount { get; init; }

    public required decimal SuggestedCostPerUsb { get; init; }
}
