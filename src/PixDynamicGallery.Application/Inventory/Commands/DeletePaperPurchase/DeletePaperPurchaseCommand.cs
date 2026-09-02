using MediatR;

namespace PixDynamicGallery.Application.Inventory.Commands.DeletePaperPurchase;

public record DeletePaperPurchaseCommand : IRequest
{
    public required Guid Id { get; init; }
}
