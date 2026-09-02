using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Application.Inventory.Commands.AddPaperPurchase;
using PixDynamicGallery.Application.Inventory.Commands.AddUsbPurchase;
using PixDynamicGallery.Application.Inventory.Commands.DeletePaperPurchase;
using PixDynamicGallery.Application.Inventory.Commands.DeleteUsbPurchase;
using PixDynamicGallery.Application.Inventory.Dtos;
using PixDynamicGallery.Application.Inventory.Queries.GetPaperStock;
using PixDynamicGallery.Application.Inventory.Queries.GetUsbStock;

namespace PixDynamicGallery.Api.Controllers;

/// <summary>Admin-only: supply stock the studio hands out or consumes at every event — photo paper/ink and USB drives, kept separate from money (<see cref="FinanceController"/>).</summary>
[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
[AdminAuth]
public class InventoryController(ISender sender) : ControllerBase
{
    /// <summary>Paper/ink inventory: purchases, remaining sheets, and the suggested cost/photo.</summary>
    [HttpGet("paper-stock")]
    [ProducesResponseType(typeof(PaperStockDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaperStockDto>> GetPaperStock(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaperStockQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("paper-purchases")]
    [ProducesResponseType(typeof(PaperPurchaseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaperPurchaseDto>> AddPaperPurchase(AddPaperPurchaseCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPaperStock), new { }, result);
    }

    [HttpDelete("paper-purchases/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePaperPurchase(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePaperPurchaseCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>USB drive inventory: purchases, remaining units, and the suggested cost/USB.</summary>
    [HttpGet("usb-stock")]
    [ProducesResponseType(typeof(UsbStockDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsbStockDto>> GetUsbStock(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUsbStockQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("usb-purchases")]
    [ProducesResponseType(typeof(UsbPurchaseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsbPurchaseDto>> AddUsbPurchase(AddUsbPurchaseCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUsbStock), new { }, result);
    }

    [HttpDelete("usb-purchases/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUsbPurchase(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUsbPurchaseCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
