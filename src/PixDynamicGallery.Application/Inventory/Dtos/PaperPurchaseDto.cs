using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Inventory.Dtos;

public record PaperPurchaseDto
{
    public required Guid Id { get; init; }

    public required DateTimeOffset PurchaseDate { get; init; }

    public required int SheetsCount { get; init; }

    public required decimal TotalCost { get; init; }

    public required decimal CostPerSheet { get; init; }

    public string? Notes { get; init; }

    public static PaperPurchaseDto FromEntity(PaperPurchase p) => new()
    {
        Id = p.Id,
        PurchaseDate = p.PurchaseDate,
        SheetsCount = p.SheetsCount,
        TotalCost = p.TotalCost,
        CostPerSheet = p.CostPerSheet,
        Notes = p.Notes,
    };
}
