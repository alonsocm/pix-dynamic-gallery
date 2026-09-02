using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Enums;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A single income or expense line for an <see cref="Event"/>. Most rows are plain manual entries
/// (a payment received, a prop bought), but two categories can be generated from a formula and then
/// hand-edited afterwards — <see cref="CreatePhotoExpense"/> (photo count × cost/photo) and
/// <see cref="CreateGasolineExpense"/> (distance × cost/km). The formula inputs are kept alongside
/// <see cref="Amount"/> purely so the UI can show "108 fotos × $7.41" even after <see cref="Update"/>
/// has changed the total — they are not re-derived from anything and are never re-computed.
/// </summary>
public class EventTransaction : BaseEntity
{
    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    public FinanceTransactionType Type { get; private set; }

    public FinanceCategory Category { get; private set; }

    public string? Description { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset TransactionDate { get; private set; }

    /// <summary>True for rows created by <see cref="CreatePhotoExpense"/>/<see cref="CreateGasolineExpense"/> — informational only, editing never clears it.</summary>
    public bool IsAutoCalculated { get; private set; }

    /// <summary>Photo count used to compute a <see cref="FinanceCategory.Photos"/> row's original amount.</summary>
    public int? PhotoCount { get; private set; }

    public decimal? CostPerPhotoSnapshot { get; private set; }

    /// <summary>Distance used to compute a <see cref="FinanceCategory.Gasoline"/> row's original amount.</summary>
    public decimal? DistanceKm { get; private set; }

    public decimal? CostPerKmSnapshot { get; private set; }

    private EventTransaction()
    {
        // Required by EF Core.
    }

    private EventTransaction(
        Guid eventId,
        FinanceTransactionType type,
        FinanceCategory category,
        string? description,
        decimal amount,
        DateTimeOffset transactionDate,
        bool isAutoCalculated,
        int? photoCount,
        decimal? costPerPhotoSnapshot,
        decimal? distanceKm,
        decimal? costPerKmSnapshot)
    {
        EventId = eventId;
        Type = type;
        Category = category;
        Description = description;
        Amount = amount;
        TransactionDate = transactionDate;
        IsAutoCalculated = isAutoCalculated;
        PhotoCount = photoCount;
        CostPerPhotoSnapshot = costPerPhotoSnapshot;
        DistanceKm = distanceKm;
        CostPerKmSnapshot = costPerKmSnapshot;
    }

    public static EventTransaction CreateManual(
        Guid eventId,
        FinanceTransactionType type,
        FinanceCategory category,
        string? description,
        decimal amount,
        DateTimeOffset transactionDate)
    {
        if (amount <= 0)
        {
            throw new DomainException("Transaction amount must be greater than zero.");
        }

        return new EventTransaction(eventId, type, category, description?.Trim(), amount, transactionDate,
            isAutoCalculated: false, photoCount: null, costPerPhotoSnapshot: null, distanceKm: null, costPerKmSnapshot: null);
    }

    /// <summary>Photo-print cost for the event: <paramref name="photoCount"/> × <paramref name="costPerPhoto"/>.</summary>
    public static EventTransaction CreatePhotoExpense(
        Guid eventId,
        int photoCount,
        decimal costPerPhoto,
        DateTimeOffset transactionDate)
    {
        if (photoCount < 0)
        {
            throw new DomainException("Photo count cannot be negative.");
        }

        if (costPerPhoto < 0)
        {
            throw new DomainException("Cost per photo cannot be negative.");
        }

        var amount = photoCount * costPerPhoto;
        return new EventTransaction(eventId, FinanceTransactionType.Expense, FinanceCategory.Photos,
            description: $"{photoCount} foto(s) × {costPerPhoto:0.00}", amount, transactionDate,
            isAutoCalculated: true, photoCount, costPerPhoto, distanceKm: null, costPerKmSnapshot: null);
    }

    /// <summary>Fuel cost for the event: <paramref name="distanceKm"/> × <paramref name="costPerKm"/>.</summary>
    public static EventTransaction CreateGasolineExpense(
        Guid eventId,
        decimal distanceKm,
        decimal costPerKm,
        DateTimeOffset transactionDate)
    {
        if (distanceKm <= 0)
        {
            throw new DomainException("Distance must be greater than zero.");
        }

        if (costPerKm < 0)
        {
            throw new DomainException("Cost per km cannot be negative.");
        }

        var amount = distanceKm * costPerKm;
        return new EventTransaction(eventId, FinanceTransactionType.Expense, FinanceCategory.Gasoline,
            description: $"{distanceKm:0.0} km × {costPerKm:0.00}", amount, transactionDate,
            isAutoCalculated: true, photoCount: null, costPerPhotoSnapshot: null, distanceKm, costPerKm);
    }

    /// <summary>Hand-edits any transaction (auto-calculated or not) — the formula snapshot, if any, is left untouched for reference.</summary>
    public void Update(decimal amount, string? description, DateTimeOffset transactionDate)
    {
        if (amount <= 0)
        {
            throw new DomainException("Transaction amount must be greater than zero.");
        }

        Amount = amount;
        Description = description?.Trim();
        TransactionDate = transactionDate;
    }
}
