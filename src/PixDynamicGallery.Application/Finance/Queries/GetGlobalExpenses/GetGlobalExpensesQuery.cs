using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Queries.GetGlobalExpenses;

/// <summary>Admin-only: every expense not tied to a specific event, newest first.</summary>
public record GetGlobalExpensesQuery : IRequest<List<GlobalExpenseDto>>;

public class GetGlobalExpensesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetGlobalExpensesQuery, List<GlobalExpenseDto>>
{
    public async Task<List<GlobalExpenseDto>> Handle(GetGlobalExpensesQuery request, CancellationToken cancellationToken)
    {
        var expenses = await context.GlobalExpenses
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(cancellationToken);

        return expenses.Select(GlobalExpenseDto.FromEntity).ToList();
    }
}
