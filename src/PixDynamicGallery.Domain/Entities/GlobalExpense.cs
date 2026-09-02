using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Enums;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A business expense not tied to any single <see cref="Event"/> (equipment, marketing, software,
/// etc.) — counted once in the global profit/loss dashboard, unlike <see cref="EventTransaction"/>
/// rows which are allocated to one event each.
/// </summary>
public class GlobalExpense : BaseEntity
{
    public DateTimeOffset ExpenseDate { get; private set; }

    public FinanceCategory Category { get; private set; }

    public string Description { get; private set; } = default!;

    public decimal Amount { get; private set; }

    private GlobalExpense()
    {
        // Required by EF Core.
    }

    private GlobalExpense(DateTimeOffset expenseDate, FinanceCategory category, string description, decimal amount)
    {
        ExpenseDate = expenseDate;
        Category = category;
        Description = description;
        Amount = amount;
    }

    public static GlobalExpense Create(DateTimeOffset expenseDate, FinanceCategory category, string description, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Expense description is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Expense amount must be greater than zero.");
        }

        return new GlobalExpense(expenseDate, category, description.Trim(), amount);
    }
}
