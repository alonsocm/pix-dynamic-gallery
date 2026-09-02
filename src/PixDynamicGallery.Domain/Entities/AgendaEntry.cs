using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Enums;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// A booking in the studio's agenda — a client and a date, tracked from first contact through to
/// the event happening. Deliberately separate from <see cref="Event"/>: an <see cref="AgendaEntry"/>
/// can exist long before there's a watch folder, a slug, or anything technical to configure, and a
/// single conversation with a client can go nowhere (<see cref="AgendaStatus.Cancelled"/>) without
/// ever needing an <see cref="Event"/> at all. Once the studio is ready to run the booth for it,
/// <see cref="LinkToEvent"/> records which <see cref="Event"/> it turned into.
/// </summary>
public class AgendaEntry : BaseEntity
{
    public string ClientName { get; private set; } = default!;

    public string? ContactPhone { get; private set; }

    public string? ContactEmail { get; private set; }

    /// <summary>Free-text kind of event, e.g. "Boda", "XV años", "Corporativo".</summary>
    public string EventType { get; private set; } = default!;

    public DateTimeOffset EventDate { get; private set; }

    public string? Location { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Price agreed with the client, if any — used only for reference; actual income is logged as <see cref="EventTransaction"/> rows once there's a linked <see cref="Event"/>.</summary>
    public decimal? AgreedPrice { get; private set; }

    public AgendaStatus Status { get; private set; }

    /// <summary>Set once the studio creates the technical <see cref="Event"/> for this booking.</summary>
    public Guid? LinkedEventId { get; private set; }

    private AgendaEntry()
    {
        // Required by EF Core.
    }

    private AgendaEntry(
        string clientName,
        string eventType,
        DateTimeOffset eventDate,
        string? contactPhone,
        string? contactEmail,
        string? location,
        string? notes,
        decimal? agreedPrice)
    {
        ClientName = clientName;
        EventType = eventType;
        EventDate = eventDate;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        Location = location;
        Notes = notes;
        AgreedPrice = agreedPrice;
        Status = AgendaStatus.Prospect;
    }

    public static AgendaEntry Create(
        string clientName,
        string eventType,
        DateTimeOffset eventDate,
        string? contactPhone,
        string? contactEmail,
        string? location,
        string? notes,
        decimal? agreedPrice)
    {
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new DomainException("Client name is required.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new DomainException("Event type is required.");
        }

        if (agreedPrice is < 0)
        {
            throw new DomainException("Agreed price cannot be negative.");
        }

        return new AgendaEntry(
            clientName.Trim(),
            eventType.Trim(),
            eventDate,
            contactPhone?.Trim(),
            contactEmail?.Trim(),
            location?.Trim(),
            notes?.Trim(),
            agreedPrice);
    }

    public void UpdateDetails(
        string clientName,
        string eventType,
        DateTimeOffset eventDate,
        string? contactPhone,
        string? contactEmail,
        string? location,
        string? notes,
        decimal? agreedPrice)
    {
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new DomainException("Client name is required.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new DomainException("Event type is required.");
        }

        if (agreedPrice is < 0)
        {
            throw new DomainException("Agreed price cannot be negative.");
        }

        ClientName = clientName.Trim();
        EventType = eventType.Trim();
        EventDate = eventDate;
        ContactPhone = contactPhone?.Trim();
        ContactEmail = contactEmail?.Trim();
        Location = location?.Trim();
        Notes = notes?.Trim();
        AgreedPrice = agreedPrice;
    }

    public void SetStatus(AgendaStatus status) => Status = status;

    /// <summary>Records that this booking turned into a real, technical <see cref="Event"/>.</summary>
    public void LinkToEvent(Guid eventId)
    {
        LinkedEventId = eventId;
        if (Status == AgendaStatus.Prospect)
        {
            Status = AgendaStatus.Confirmed;
        }
    }
}
