using MediatR;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteUsbPurchase;

public record DeleteUsbPurchaseCommand : IRequest
{
    public required Guid Id { get; init; }
}
