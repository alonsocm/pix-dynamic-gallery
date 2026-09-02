using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Commands.AddGlobalExpense;

/// <summary>A business expense not tied to any single event (equipment, marketing, etc.).</summary>
public record AddGlobalExpenseCommand : IRequest<GlobalExpenseDto>
{
    public required DateTimeOffset ExpenseDate { get; init; }

    public required FinanceCategory Category { get; init; }

    public required string Description { get; init; }

    public required decimal Amount { get; init; }
}
