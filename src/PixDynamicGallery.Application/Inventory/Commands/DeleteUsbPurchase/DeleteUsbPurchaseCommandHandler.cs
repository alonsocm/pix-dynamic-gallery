using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Inventory.Commands.DeleteUsbPurchase;

public class DeleteUsbPurchaseCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteUsbPurchaseCommand>
{
    public async Task Handle(DeleteUsbPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await context.UsbPurchases.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.UsbPurchase), request.Id);

        context.UsbPurchases.Remove(purchase);
        await context.SaveChangesAsync(cancellationToken);
    }
}
