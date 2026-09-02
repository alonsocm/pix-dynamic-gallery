using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Agenda.Dtos;

public record AgendaEntryDto
{
    public required Guid Id { get; init; }

    public required string ClientName { get; init; }

    public string? ContactPhone { get; init; }

    public string? ContactEmail { get; init; }

    public required string EventType { get; init; }

    public required DateTimeOffset EventDate { get; init; }

    public string? Location { get; init; }

    public string? Notes { get; init; }

    public decimal? AgreedPrice { get; init; }

    public required AgendaStatus Status { get; init; }

    public Guid? LinkedEventId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static AgendaEntryDto FromEntity(AgendaEntry entry) => new()
    {
        Id = entry.Id,
        ClientName = entry.ClientName,
        ContactPhone = entry.ContactPhone,
        ContactEmail = entry.ContactEmail,
        EventType = entry.EventType,
        EventDate = entry.EventDate,
        Location = entry.Location,
        Notes = entry.Notes,
        AgreedPrice = entry.AgreedPrice,
        Status = entry.Status,
        LinkedEventId = entry.LinkedEventId,
        CreatedAtUtc = entry.CreatedAtUtc,
    };
}
