using MediatR;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddPaperPurchase;

public class AddPaperPurchaseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddPaperPurchaseCommand, PaperPurchaseDto>
{
    public async Task<PaperPurchaseDto> Handle(AddPaperPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = PaperPurchase.Create(request.PurchaseDate, request.SheetsCount, request.TotalCost, request.Notes);

        context.PaperPurchases.Add(purchase);
        await context.SaveChangesAsync(cancellationToken);

        return PaperPurchaseDto.FromEntity(purchase);
    }
}
