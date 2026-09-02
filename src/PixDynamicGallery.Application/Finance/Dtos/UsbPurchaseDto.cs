using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Dtos;

public record UsbPurchaseDto
{
    public required Guid Id { get; init; }

    public required DateTimeOffset PurchaseDate { get; init; }

    public required int UnitsCount { get; init; }

    public required decimal TotalCost { get; init; }

    public required decimal CostPerUnit { get; init; }

    public string? Notes { get; init; }

    public static UsbPurchaseDto FromEntity(UsbPurchase p) => new()
    {
        Id = p.Id,
        PurchaseDate = p.PurchaseDate,
        UnitsCount = p.UnitsCount,
        TotalCost = p.TotalCost,
        CostPerUnit = p.CostPerUnit,
        Notes = p.Notes,
    };
}
