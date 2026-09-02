using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Queries.GetFinanceDashboard;

/// <summary>Admin-only: global balance and reports across every event — powers /admin/finance. Stock/inventory lives in /admin/inventory instead (see InventoryController).</summary>
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
        var usbPurchasesTotal = await context.UsbPurchases.SumAsync(p => (decimal?)p.TotalCost, cancellationToken) ?? 0m;

        // This studio only creates an agenda entry once a deposit has actually been received, so an
        // untransferred deposit (no linked Event yet) is still real income — see FinanceDashboardDto.
        var pendingDeposits = await (
            from deposit in context.AgendaDeposits
            where deposit.TransferredTransactionId == null
            join entry in context.AgendaEntries on deposit.AgendaEntryId equals entry.Id
            select new { entry.Id, entry.ClientName, entry.EventDate, deposit.Amount, deposit.PaymentDate })
            .ToListAsync(cancellationToken);

        var agendaDeposits = pendingDeposits
            .GroupBy(d => new { d.Id, d.ClientName, d.EventDate })
            .Select(g => new PendingAgendaDepositDto
            {
                AgendaEntryId = g.Key.Id,
                ClientName = g.Key.ClientName,
                EventDate = g.Key.EventDate,
                Total = g.Sum(d => d.Amount),
            })
            .OrderBy(d => d.EventDate)
            .ToList();
        var pendingDepositsTotal = agendaDeposits.Sum(d => d.Total);

        var totalIncome = allTransactions.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount) + pendingDepositsTotal;

        // Real cash out: global expenses + paper/USB purchases + event expenses that aren't Photos or
        // Usb. Those two categories are excluded — they allocate spend already counted in
        // paperPurchasesTotal/usbPurchasesTotal to a specific event, not new money out (see
        // FinanceDashboardDto's doc comment).
        var otherEventExpenses = allTransactions
            .Where(t => t.Type == FinanceTransactionType.Expense && t.Category != FinanceCategory.Photos && t.Category != FinanceCategory.Usb)
            .Sum(t => t.Amount);
        var totalRealExpenses = globalExpensesTotal + paperPurchasesTotal + usbPurchasesTotal + otherEventExpenses;

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

        // Income by month: every Income EventTransaction plus every still-pending agenda deposit,
        // each counted in the month its money actually came in (TransactionDate/PaymentDate).
        var incomeDates = allTransactions
            .Where(t => t.Type == FinanceTransactionType.Income)
            .Select(t => (Date: t.TransactionDate, t.Amount))
            .Concat(pendingDeposits.Select(d => (Date: d.PaymentDate, d.Amount)));

        var incomeByMonth = incomeDates
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new MonthlyAmountDto { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => x.Amount) })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        // Events by month: agenda bookings by their event date, excluding cancelled ones — this is
        // the only place a date lives (the technical Event has none), and matches how this studio
        // agendas (see AgendaEntry's doc comment).
        var agendaEntries = await context.AgendaEntries
            .Where(a => a.Status != AgendaStatus.Cancelled)
            .Select(a => new { a.EventDate })
            .ToListAsync(cancellationToken);

        var eventsByMonth = agendaEntries
            .GroupBy(a => new { a.EventDate.Year, a.EventDate.Month })
            .Select(g => new MonthlyCountDto { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        return new FinanceDashboardDto
        {
            TotalIncome = totalIncome,
            TotalRealExpenses = totalRealExpenses,
            NetProfit = totalIncome - totalRealExpenses,
            PerEventBreakdown = perEventBreakdown,
            PendingDepositsTotal = pendingDepositsTotal,
            AgendaDeposits = agendaDeposits,
            IncomeByMonth = incomeByMonth,
            EventsByMonth = eventsByMonth,
        };
    }
}
