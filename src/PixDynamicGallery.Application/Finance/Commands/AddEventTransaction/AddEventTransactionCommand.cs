using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Commands.AddEventTransaction;

/// <summary>Manual income/expense entry — payments, tips, props bought, etc.</summary>
public record AddEventTransactionCommand : IRequest<EventTransactionDto>
{
    public required Guid EventId { get; init; }

    public required FinanceTransactionType Type { get; init; }

    public required FinanceCategory Category { get; init; }

    public string? Description { get; init; }

    public required decimal Amount { get; init; }

    public required DateTimeOffset TransactionDate { get; init; }
}
