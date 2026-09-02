using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.AddPaperPurchase;

public record AddPaperPurchaseCommand : IRequest<PaperPurchaseDto>
{
    public required DateTimeOffset PurchaseDate { get; init; }

    public required int SheetsCount { get; init; }

    public required decimal TotalCost { get; init; }

    public string? Notes { get; init; }
}
