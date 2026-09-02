using MediatR;

namespace PixDynamicGallery.Application.Finance.Commands.DeletePaperPurchase;

public record DeletePaperPurchaseCommand : IRequest
{
    public required Guid Id { get; init; }
}
