using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Application.Agenda.Commands.AddAgendaDeposit;
using PixDynamicGallery.Application.Agenda.Commands.CreateAgendaEntry;
using PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaDeposit;
using PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaEntry;
using PixDynamicGallery.Application.Agenda.Commands.LinkAgendaEntryToEvent;
using PixDynamicGallery.Application.Agenda.Commands.SetAgendaStatus;
using PixDynamicGallery.Application.Agenda.Commands.UpdateAgendaEntry;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Agenda.Queries.GetAgendaEntries;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Api.Controllers;

/// <summary>Admin-only: the studio's booking agenda, tracked independently of the technical Event it may later turn into.</summary>
[ApiController]
[Route("api/agenda")]
[Produces("application/json")]
[AdminAuth]
public class AgendaController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AgendaEntryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AgendaEntryDto>> Create(CreateAgendaEntryCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    /// <summary>Every booking, optionally filtered by date range and/or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AgendaEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaEntryDto>>> GetAll(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] AgendaStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAgendaEntriesQuery { From = from, To = to, Status = status }, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AgendaEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaEntryDto>> Update(Guid id, UpdateAgendaEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateAgendaEntryCommand
            {
                Id = id,
                ClientName = request.ClientName,
                EventType = request.EventType,
                EventDate = request.EventDate,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Location = request.Location,
                Notes = request.Notes,
                AgreedPrice = request.AgreedPrice,
            },
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AgendaEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaEntryDto>> SetStatus(Guid id, SetAgendaStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetAgendaStatusCommand { Id = id, Status = request.Status }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Records which technical Event this booking turned into.</summary>
    [HttpPost("{id:guid}/link-event")]
    [ProducesResponseType(typeof(AgendaEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaEntryDto>> LinkToEvent(Guid id, LinkAgendaEntryToEventRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LinkAgendaEntryToEventCommand { AgendaEntryId = id, EventId = request.EventId }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAgendaEntryCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Logs a deposit/advance payment for the booking. If it's already linked to a technical Event, the deposit is booked as income on that event immediately.</summary>
    [HttpPost("{id:guid}/deposits")]
    [ProducesResponseType(typeof(AgendaDepositDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaDepositDto>> AddDeposit(Guid id, AddAgendaDepositRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddAgendaDepositCommand { AgendaEntryId = id, Amount = request.Amount, PaymentDate = request.PaymentDate, Notes = request.Notes },
            cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    /// <summary>Removes a deposit — refused (400) once it's been converted into an event income transaction; delete that transaction from the event's finance screen instead.</summary>
    [HttpDelete("{id:guid}/deposits/{depositId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDeposit(Guid id, Guid depositId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAgendaDepositCommand { Id = depositId }, cancellationToken);
        return NoContent();
    }
}

public record AddAgendaDepositRequest(decimal Amount, DateTimeOffset PaymentDate, string? Notes);

public record UpdateAgendaEntryRequest(
    string ClientName,
    string EventType,
    DateTimeOffset EventDate,
    string? ContactPhone,
    string? ContactEmail,
    string? Location,
    string? Notes,
    decimal? AgreedPrice);

public record SetAgendaStatusRequest(AgendaStatus Status);

public record LinkAgendaEntryToEventRequest(Guid EventId);
