using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Finance.Commands.DeletePaperPurchase;

public class DeletePaperPurchaseCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePaperPurchaseCommand>
{
    public async Task Handle(DeletePaperPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await context.PaperPurchases.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.PaperPurchase), request.Id);

        context.PaperPurchases.Remove(purchase);
        await context.SaveChangesAsync(cancellationToken);
    }
}
