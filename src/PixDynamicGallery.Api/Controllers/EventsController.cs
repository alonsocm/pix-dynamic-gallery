using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Application.Events.Commands.CreateEvent;
using PixDynamicGallery.Application.Events.Commands.SetEventActive;
using PixDynamicGallery.Application.Events.Dtos;
using PixDynamicGallery.Application.Events.Queries.GetAllEvents;
using PixDynamicGallery.Application.Events.Queries.GetEventBySlug;

namespace PixDynamicGallery.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new event and its associated Sparkbooth watch-folder binding.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> Create(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>Resolves an event by its URL slug — used by the kiosk, guest landing page and live wall on load.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventBySlugQuery(slug), cancellationToken);
        return Ok(result);
    }

    /// <summary>Admin-only: every event, newest first — powers the /admin/events screen.</summary>
    [HttpGet]
    [AdminAuth]
    [ProducesResponseType(typeof(List<AdminEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AdminEventDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllEventsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Admin-only: activates or deactivates an event (stops/resumes the watcher for it).</summary>
    [HttpPatch("{id:guid}/active")]
    [AdminAuth]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> SetActive(Guid id, SetEventActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetEventActiveCommand { EventId = id, IsActive = request.IsActive }, cancellationToken);
        return Ok(result);
    }
}

public record SetEventActiveRequest(bool IsActive);
