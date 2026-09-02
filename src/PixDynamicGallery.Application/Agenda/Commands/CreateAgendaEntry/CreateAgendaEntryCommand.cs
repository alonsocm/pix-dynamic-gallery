using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;

namespace PixDynamicGallery.Application.Agenda.Commands.CreateAgendaEntry;

/// <summary>Adds a booking to the agenda — the first record of a prospect/client before any technical Event exists.</summary>
public record CreateAgendaEntryCommand : IRequest<AgendaEntryDto>
{
    public required string ClientName { get; init; }

    public required string EventType { get; init; }

    public required DateTimeOffset EventDate { get; init; }

    public string? ContactPhone { get; init; }

    public string? ContactEmail { get; init; }

    public string? Location { get; init; }

    public string? Notes { get; init; }

    public decimal? AgreedPrice { get; init; }
}
