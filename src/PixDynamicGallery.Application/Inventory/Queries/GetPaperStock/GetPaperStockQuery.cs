using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Inventory.Dtos;

namespace PixDynamicGallery.Application.Inventory.Queries.GetPaperStock;

/// <summary>Admin-only: paper/ink inventory (purchases, remaining sheets, suggested cost/photo) — powers the /admin/finance stock card.</summary>
public record GetPaperStockQuery : IRequest<PaperStockDto>;

public class GetPaperStockQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPaperStockQuery, PaperStockDto>
{
    public async Task<PaperStockDto> Handle(GetPaperStockQuery request, CancellationToken cancellationToken)
    {
        var purchases = await context.PaperPurchases
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync(cancellationToken);

        var totalPurchased = purchases.Sum(p => p.SheetsCount);
        var totalConsumed = await PaperStockCalculator.GetTotalConsumedSheetsAsync(context, cancellationToken);
        var suggestedCostPerPhoto = await PaperStockCalculator.GetSuggestedCostPerPhotoAsync(context, cancellationToken);

        return new PaperStockDto
        {
            Purchases = purchases.Select(PaperPurchaseDto.FromEntity).ToList(),
            TotalPurchasedSheets = totalPurchased,
            TotalConsumedSheets = totalConsumed,
            RemainingSheets = totalPurchased - totalConsumed,
            SuggestedCostPerPhoto = suggestedCostPerPhoto,
        };
    }
}
