using MediatR;
using PixDynamicGallery.Application.Agenda.Dtos;

namespace PixDynamicGallery.Application.Agenda.Commands.UpdateAgendaEntry;

public record UpdateAgendaEntryCommand : IRequest<AgendaEntryDto>
{
    public required Guid Id { get; init; }

    public required string ClientName { get; init; }

    public required string EventType { get; init; }

    public required DateTimeOffset EventDate { get; init; }

    public string? ContactPhone { get; init; }

    public string? ContactEmail { get; init; }

    public string? Location { get; init; }

    public string? Notes { get; init; }

    public decimal? AgreedPrice { get; init; }
}
