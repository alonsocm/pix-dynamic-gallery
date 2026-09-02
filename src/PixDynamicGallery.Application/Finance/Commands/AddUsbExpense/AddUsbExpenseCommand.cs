using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.AddUsbExpense;

/// <summary>
/// Auto-calculates the USB drive expense for an event: USB count × cost/USB. Both default (count
/// to 1 — one drive handed out per event, cost/USB to the latest USB purchase's cost/unit) but can
/// be overridden.
/// </summary>
public record AddUsbExpenseCommand : IRequest<EventTransactionDto>
{
    public required Guid EventId { get; init; }

    public int? UsbCount { get; init; }

    public decimal? CostPerUsb { get; init; }

    public DateTimeOffset? TransactionDate { get; init; }
}
