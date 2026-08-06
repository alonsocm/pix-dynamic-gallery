using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Application.Common.Models;
using PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;
using PixDynamicGallery.Application.Photos.Dtos;
using PixDynamicGallery.Application.Photos.Queries.GetEventPhotos;
using PixDynamicGallery.Application.Photos.Queries.GetPhotoById;

namespace PixDynamicGallery.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/photos")]
[Produces("application/json")]
public class PhotosController(ISender sender) : ControllerBase
{
    private const long MaxUploadBytes = 50 * 1024 * 1024; // 50 MB — comfortably covers Sparkbooth GIFs.

    /// <summary>Paginated feed of uploaded photos for an event, newest first — powers the <c>/e/:eventId/wall</c> live wall.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PhotoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<PhotoDto>>> GetEventPhotos(
        Guid eventId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetEventPhotosQuery { EventId = eventId, PageNumber = pageNumber, PageSize = pageSize },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Single photo's details — backs the guest landing page <c>/e/:eventId/p/:photoId</c>.</summary>
    [HttpGet("{photoId:guid}")]
    [ProducesResponseType(typeof(PhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhotoDto>> GetPhoto(Guid eventId, Guid photoId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPhotoByIdQuery(eventId, photoId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Manual ingestion endpoint: uploads a file through the exact same pipeline
    /// <see cref="Infrastructure.Watcher.SparkboothWatcherService" /> uses, without needing Sparkbooth
    /// hardware attached. Handy for testing from Swagger and as a fallback capture path.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(PhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PhotoDto>> UploadPhoto(Guid eventId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("The uploaded file is empty.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "pix-dynamic-gallery-uploads");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

        try
        {
            await using (var stream = System.IO.File.Create(tempFilePath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var result = await sender.Send(
                new UploadCapturedPhotoCommand { EventId = eventId, LocalFilePath = tempFilePath },
                cancellationToken);

            return CreatedAtAction(nameof(GetPhoto), new { eventId, photoId = result.Id }, result);
        }
        finally
        {
            // This endpoint bypasses Sparkbooth's own local archive, so the temp copy is disposable
            // once the command has read it into cloud storage.
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }
}
