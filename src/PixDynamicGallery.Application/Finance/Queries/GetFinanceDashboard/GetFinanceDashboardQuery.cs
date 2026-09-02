using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Queries.GetFinanceDashboard;

/// <summary>Admin-only: global profit/loss overview across every event — powers /admin/finance.</summary>
public record GetFinanceDashboardQuery : IRequest<FinanceDashboardDto>;

public class GetFinanceDashboardQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFinanceDashboardQuery, FinanceDashboardDto>
{
    public async Task<FinanceDashboardDto> Handle(GetFinanceDashboardQuery request, CancellationToken cancellationToken)
    {
        var allTransactions = await context.EventTransactions.ToListAsync(cancellationToken);
        var events = await context.Events.ToListAsync(cancellationToken);
        var globalExpensesTotal = await context.GlobalExpenses.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var paperPurchasesTotal = await context.PaperPurchases.SumAsync(p => (decimal?)p.TotalCost, cancellationToken) ?? 0m;

        var totalIncome = allTransactions.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);

        // Real cash out: global expenses + paper purchases + non-Photos event expenses. Photos-category
        // event expenses are excluded — they allocate spend already counted in paperPurchasesTotal to a
        // specific event, not new money out (see FinanceDashboardDto's doc comment).
        var nonPhotoEventExpenses = allTransactions
            .Where(t => t.Type == FinanceTransactionType.Expense && t.Category != FinanceCategory.Photos)
            .Sum(t => t.Amount);
        var totalRealExpenses = globalExpensesTotal + paperPurchasesTotal + nonPhotoEventExpenses;

        var perEventBreakdown = events
            .Select(e =>
            {
                var eventTransactions = allTransactions.Where(t => t.EventId == e.Id).ToList();
                var income = eventTransactions.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);
                var expense = eventTransactions.Where(t => t.Type == FinanceTransactionType.Expense).Sum(t => t.Amount);

                return new PerEventBreakdownDto
                {
                    EventId = e.Id,
                    EventName = e.Name,
                    Income = income,
                    Expense = expense,
                    Profit = income - expense,
                };
            })
            .Where(b => b.Income != 0 || b.Expense != 0)
            .OrderByDescending(b => b.Profit)
            .ToList();

        var purchases = await context.PaperPurchases.OrderByDescending(p => p.PurchaseDate).ToListAsync(cancellationToken);
        var totalConsumed = await PaperStockCalculator.GetTotalConsumedSheetsAsync(context, cancellationToken);
        var totalPurchasedSheets = purchases.Sum(p => p.SheetsCount);

        var paperStock = new PaperStockDto
        {
            Purchases = purchases.Select(PaperPurchaseDto.FromEntity).ToList(),
            TotalPurchasedSheets = totalPurchasedSheets,
            TotalConsumedSheets = totalConsumed,
            RemainingSheets = totalPurchasedSheets - totalConsumed,
            SuggestedCostPerPhoto = await PaperStockCalculator.GetSuggestedCostPerPhotoAsync(context, cancellationToken),
        };

        return new FinanceDashboardDto
        {
            TotalIncome = totalIncome,
            TotalRealExpenses = totalRealExpenses,
            NetProfit = totalIncome - totalRealExpenses,
            PerEventBreakdown = perEventBreakdown,
            PaperStock = paperStock,
        };
    }
}
