using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Application.Events.Commands.CreateEvent;
using PixDynamicGallery.Application.Events.Dtos;
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
}
