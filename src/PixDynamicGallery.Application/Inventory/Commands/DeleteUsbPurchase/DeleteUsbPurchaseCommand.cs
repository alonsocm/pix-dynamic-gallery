using MediatR;

namespace PixDynamicGallery.Application.Inventory.Commands.DeleteUsbPurchase;

public record DeleteUsbPurchaseCommand : IRequest
{
    public required Guid Id { get; init; }
}
