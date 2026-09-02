using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Dtos;

public record EventTransactionDto
{
    public required Guid Id { get; init; }

    public required Guid EventId { get; init; }

    public required FinanceTransactionType Type { get; init; }

    public required FinanceCategory Category { get; init; }

    public string? Description { get; init; }

    public required decimal Amount { get; init; }

    public required DateTimeOffset TransactionDate { get; init; }

    public required bool IsAutoCalculated { get; init; }

    public int? PhotoCount { get; init; }

    public decimal? CostPerPhotoSnapshot { get; init; }

    public decimal? DistanceKm { get; init; }

    public decimal? CostPerKmSnapshot { get; init; }

    public int? UsbCount { get; init; }

    public decimal? CostPerUsbSnapshot { get; init; }

    public static EventTransactionDto FromEntity(EventTransaction t) => new()
    {
        Id = t.Id,
        EventId = t.EventId,
        Type = t.Type,
        Category = t.Category,
        Description = t.Description,
        Amount = t.Amount,
        TransactionDate = t.TransactionDate,
        IsAutoCalculated = t.IsAutoCalculated,
        PhotoCount = t.PhotoCount,
        CostPerPhotoSnapshot = t.CostPerPhotoSnapshot,
        DistanceKm = t.DistanceKm,
        CostPerKmSnapshot = t.CostPerKmSnapshot,
        UsbCount = t.UsbCount,
        CostPerUsbSnapshot = t.CostPerUsbSnapshot,
    };
}
