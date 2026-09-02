using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Application.Finance.Commands.AddEventTransaction;
using PixDynamicGallery.Application.Finance.Commands.AddGasolineExpense;
using PixDynamicGallery.Application.Finance.Commands.AddPhotoExpense;
using PixDynamicGallery.Application.Finance.Commands.DeleteEventTransaction;
using PixDynamicGallery.Application.Finance.Commands.UpdateEventTransaction;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Application.Finance.Queries.GetEventFinanceSummary;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Api.Controllers;

/// <summary>Admin-only: income/expense tracking for one event — its own P&amp;L, including the auto-calculated photo and gasoline expenses.</summary>
[ApiController]
[Route("api/events/{eventId:guid}/transactions")]
[Produces("application/json")]
[AdminAuth]
public class EventTransactionsController(ISender sender) : ControllerBase
{
    /// <summary>Every transaction for the event, its totals, and the current suggested photo/km rates.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(EventFinanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventFinanceSummaryDto>> GetAll(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventFinanceSummaryQuery(eventId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Manual entry — a payment, a tip, an ad-hoc expense.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTransactionDto>> Create(Guid eventId, AddEventTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddEventTransactionCommand
            {
                EventId = eventId,
                Type = request.Type,
                Category = request.Category,
                Description = request.Description,
                Amount = request.Amount,
                TransactionDate = request.TransactionDate,
            },
            cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { eventId }, result);
    }

    /// <summary>Auto-calculates the photo-print expense: photo count × cost/photo. Both default (uploaded photo count, latest paper purchase's cost/sheet) but can be overridden.</summary>
    [HttpPost("photo-expense")]
    [ProducesResponseType(typeof(EventTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTransactionDto>> AddPhotoExpense(Guid eventId, AddPhotoExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddPhotoExpenseCommand
            {
                EventId = eventId,
                PhotoCount = request.PhotoCount,
                CostPerPhoto = request.CostPerPhoto,
                TransactionDate = request.TransactionDate,
            },
            cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { eventId }, result);
    }

    /// <summary>Auto-calculates the fuel expense: distance × cost/km. Cost/km defaults to the studio-wide setting but can be overridden.</summary>
    [HttpPost("gasoline-expense")]
    [ProducesResponseType(typeof(EventTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTransactionDto>> AddGasolineExpense(Guid eventId, AddGasolineExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddGasolineExpenseCommand
            {
                EventId = eventId,
                DistanceKm = request.DistanceKm,
                CostPerKm = request.CostPerKm,
                TransactionDate = request.TransactionDate,
            },
            cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { eventId }, result);
    }

    /// <summary>Hand-edits any transaction, including auto-calculated ones.</summary>
    [HttpPatch("{transactionId:guid}")]
    [ProducesResponseType(typeof(EventTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTransactionDto>> Update(Guid eventId, Guid transactionId, UpdateEventTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateEventTransactionCommand
            {
                Id = transactionId,
                Amount = request.Amount,
                Description = request.Description,
                TransactionDate = request.TransactionDate,
            },
            cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid eventId, Guid transactionId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteEventTransactionCommand { Id = transactionId }, cancellationToken);
        return NoContent();
    }
}

public record AddEventTransactionRequest(FinanceTransactionType Type, FinanceCategory Category, string? Description, decimal Amount, DateTimeOffset TransactionDate);

public record AddPhotoExpenseRequest(int? PhotoCount, decimal? CostPerPhoto, DateTimeOffset? TransactionDate);

public record AddGasolineExpenseRequest(decimal DistanceKm, decimal? CostPerKm, DateTimeOffset? TransactionDate);

public record UpdateEventTransactionRequest(decimal Amount, string? Description, DateTimeOffset TransactionDate);
