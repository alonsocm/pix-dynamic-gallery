using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.AddGasolineExpense;

/// <summary>Auto-calculates the fuel expense for an event: distance × cost/km. Cost/km defaults to <see cref="Domain.Entities.FinanceSettings.CostPerKm"/> but can be overridden.</summary>
public record AddGasolineExpenseCommand : IRequest<EventTransactionDto>
{
    public required Guid EventId { get; init; }

    public required decimal DistanceKm { get; init; }

    public decimal? CostPerKm { get; init; }

    public DateTimeOffset? TransactionDate { get; init; }
}
