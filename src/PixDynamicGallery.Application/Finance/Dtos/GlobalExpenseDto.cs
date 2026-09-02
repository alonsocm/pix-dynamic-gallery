using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Dtos;

public record GlobalExpenseDto
{
    public required Guid Id { get; init; }

    public required DateTimeOffset ExpenseDate { get; init; }

    public required FinanceCategory Category { get; init; }

    public required string Description { get; init; }

    public required decimal Amount { get; init; }

    public static GlobalExpenseDto FromEntity(GlobalExpense e) => new()
    {
        Id = e.Id,
        ExpenseDate = e.ExpenseDate,
        Category = e.Category,
        Description = e.Description,
        Amount = e.Amount,
    };
}
