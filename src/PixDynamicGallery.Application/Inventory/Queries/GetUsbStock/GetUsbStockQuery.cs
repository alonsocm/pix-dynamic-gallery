using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Inventory.Dtos;

namespace PixDynamicGallery.Application.Inventory.Queries.GetUsbStock;

/// <summary>Admin-only: USB drive inventory (purchases, remaining units, suggested cost/USB) — powers the /admin/finance stock card.</summary>
public record GetUsbStockQuery : IRequest<UsbStockDto>;

public class GetUsbStockQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUsbStockQuery, UsbStockDto>
{
    public async Task<UsbStockDto> Handle(GetUsbStockQuery request, CancellationToken cancellationToken)
    {
        var purchases = await context.UsbPurchases
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync(cancellationToken);

        var totalPurchased = purchases.Sum(p => p.UnitsCount);
        var totalConsumed = await UsbStockCalculator.GetTotalConsumedUnitsAsync(context, cancellationToken);
        var suggestedCostPerUsb = await UsbStockCalculator.GetSuggestedCostPerUsbAsync(context, cancellationToken);

        return new UsbStockDto
        {
            Purchases = purchases.Select(UsbPurchaseDto.FromEntity).ToList(),
            TotalPurchasedUnits = totalPurchased,
            TotalConsumedUnits = totalConsumed,
            RemainingUnits = totalPurchased - totalConsumed,
            SuggestedCostPerUsb = suggestedCostPerUsb,
        };
    }
}
