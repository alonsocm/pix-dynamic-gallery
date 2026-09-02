using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.AddPhotoExpense;

/// <summary>
/// Auto-calculates the photo-print expense for an event: photo count × cost/photo. Both inputs
/// default (photo count to the event's uploaded photos, cost/photo to the latest paper purchase's
/// cost/sheet) but can be overridden — e.g. if not every captured photo was actually printed, or
/// the operator wants to use a different rate.
/// </summary>
public record AddPhotoExpenseCommand : IRequest<EventTransactionDto>
{
    public required Guid EventId { get; init; }

    public int? PhotoCount { get; init; }

    public decimal? CostPerPhoto { get; init; }

    public DateTimeOffset? TransactionDate { get; init; }
}
