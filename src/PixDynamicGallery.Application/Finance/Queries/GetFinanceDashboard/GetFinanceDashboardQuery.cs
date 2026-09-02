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
        var usbPurchasesTotal = await context.UsbPurchases.SumAsync(p => (decimal?)p.TotalCost, cancellationToken) ?? 0m;

        // This studio only creates an agenda entry once a deposit has actually been received, so an
        // untransferred deposit (no linked Event yet) is still real income — see FinanceDashboardDto.
        var pendingDeposits = await (
            from deposit in context.AgendaDeposits
            where deposit.TransferredTransactionId == null
            join entry in context.AgendaEntries on deposit.AgendaEntryId equals entry.Id
            select new { entry.Id, entry.ClientName, entry.EventDate, deposit.Amount })
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

        var usbPurchases = await context.UsbPurchases.OrderByDescending(p => p.PurchaseDate).ToListAsync(cancellationToken);
        var totalConsumedUsb = await UsbStockCalculator.GetTotalConsumedUnitsAsync(context, cancellationToken);
        var totalPurchasedUnits = usbPurchases.Sum(p => p.UnitsCount);

        var usbStock = new UsbStockDto
        {
            Purchases = usbPurchases.Select(UsbPurchaseDto.FromEntity).ToList(),
            TotalPurchasedUnits = totalPurchasedUnits,
            TotalConsumedUnits = totalConsumedUsb,
            RemainingUnits = totalPurchasedUnits - totalConsumedUsb,
            SuggestedCostPerUsb = await UsbStockCalculator.GetSuggestedCostPerUsbAsync(context, cancellationToken),
        };

        return new FinanceDashboardDto
        {
            TotalIncome = totalIncome,
            TotalRealExpenses = totalRealExpenses,
            NetProfit = totalIncome - totalRealExpenses,
            PerEventBreakdown = perEventBreakdown,
            PaperStock = paperStock,
            UsbStock = usbStock,
            PendingDepositsTotal = pendingDepositsTotal,
            AgendaDeposits = agendaDeposits,
        };
    }
}
