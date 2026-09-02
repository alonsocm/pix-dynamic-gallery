using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateEventTransaction;

/// <summary>Hand-edits any transaction, including auto-calculated ones — the formula snapshot (photo count/cost, distance/cost) is kept for reference but never re-applied.</summary>
public record UpdateEventTransactionCommand : IRequest<EventTransactionDto>
{
    public required Guid Id { get; init; }

    public required decimal Amount { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset TransactionDate { get; init; }
}
