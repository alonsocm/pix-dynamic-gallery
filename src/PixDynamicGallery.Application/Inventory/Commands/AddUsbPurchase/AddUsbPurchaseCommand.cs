using MediatR;
using PixDynamicGallery.Application.Inventory.Dtos;

namespace PixDynamicGallery.Application.Inventory.Commands.AddUsbPurchase;

public record AddUsbPurchaseCommand : IRequest<UsbPurchaseDto>
{
    public required DateTimeOffset PurchaseDate { get; init; }

    public required int UnitsCount { get; init; }

    public required decimal TotalCost { get; init; }

    public string? Notes { get; init; }
}
