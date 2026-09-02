using MediatR;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddUsbPurchase;

public class AddUsbPurchaseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddUsbPurchaseCommand, UsbPurchaseDto>
{
    public async Task<UsbPurchaseDto> Handle(AddUsbPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = UsbPurchase.Create(request.PurchaseDate, request.UnitsCount, request.TotalCost, request.Notes);

        context.UsbPurchases.Add(purchase);
        await context.SaveChangesAsync(cancellationToken);

        return UsbPurchaseDto.FromEntity(purchase);
    }
}
