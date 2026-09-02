using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Queries.GetEventFinanceSummary;

/// <summary>Admin-only: every transaction for one event, its totals, and the current suggested rates — powers /admin/events/:eventId/finance.</summary>
public record GetEventFinanceSummaryQuery(Guid EventId) : IRequest<EventFinanceSummaryDto>;

public class GetEventFinanceSummaryQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetEventFinanceSummaryQuery, EventFinanceSummaryDto>
{
    public async Task<EventFinanceSummaryDto> Handle(GetEventFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        var transactions = await context.EventTransactions
            .Where(t => t.EventId == request.EventId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var totalIncome = transactions.Where(t => t.Type == FinanceTransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == FinanceTransactionType.Expense).Sum(t => t.Amount);

        var suggestedPhotoCount = await context.Photos
            .CountAsync(p => p.EventId == request.EventId && p.Status == Domain.Enums.PhotoStatus.Uploaded, cancellationToken);
        var suggestedCostPerPhoto = await PaperStockCalculator.GetSuggestedCostPerPhotoAsync(context, cancellationToken);
        var suggestedCostPerKm = (await context.FinanceSettings.FirstAsync(cancellationToken)).CostPerKm;
        var suggestedCostPerUsb = await UsbStockCalculator.GetSuggestedCostPerUsbAsync(context, cancellationToken);

        return new EventFinanceSummaryDto
        {
            EventId = request.EventId,
            Transactions = transactions.Select(Dtos.EventTransactionDto.FromEntity).ToList(),
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Profit = totalIncome - totalExpense,
            SuggestedPhotoCount = suggestedPhotoCount,
            SuggestedCostPerPhoto = suggestedCostPerPhoto,
            SuggestedCostPerKm = suggestedCostPerKm,
            SuggestedUsbCount = 1,
            SuggestedCostPerUsb = suggestedCostPerUsb,
        };
    }
}
