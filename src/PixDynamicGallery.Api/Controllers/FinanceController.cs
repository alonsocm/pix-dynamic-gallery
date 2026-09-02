using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Application.Finance.Commands.AddGlobalExpense;
using PixDynamicGallery.Application.Finance.Commands.AddPaperPurchase;
using PixDynamicGallery.Application.Finance.Commands.DeleteGlobalExpense;
using PixDynamicGallery.Application.Finance.Commands.UpdateFinanceSettings;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Application.Finance.Queries.GetFinanceDashboard;
using PixDynamicGallery.Application.Finance.Queries.GetFinanceSettings;
using PixDynamicGallery.Application.Finance.Queries.GetGlobalExpenses;
using PixDynamicGallery.Application.Finance.Queries.GetPaperStock;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Api.Controllers;

/// <summary>Admin-only: studio-wide finance — the global P&amp;L dashboard, the paper/ink inventory, expenses not tied to one event, and calculation settings.</summary>
[ApiController]
[Route("api/finance")]
[Produces("application/json")]
[AdminAuth]
public class FinanceController(ISender sender) : ControllerBase
{
    /// <summary>Global income/expense totals across every event, plus the per-event breakdown and paper stock.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(FinanceDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFinanceDashboardQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(FinanceSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFinanceSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(FinanceSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceSettingsDto>> UpdateSettings(UpdateFinanceSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

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

    /// <summary>Business expenses not tied to a specific event (equipment, marketing, etc.).</summary>
    [HttpGet("global-expenses")]
    [ProducesResponseType(typeof(List<GlobalExpenseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GlobalExpenseDto>>> GetGlobalExpenses(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetGlobalExpensesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("global-expenses")]
    [ProducesResponseType(typeof(GlobalExpenseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GlobalExpenseDto>> AddGlobalExpense(AddGlobalExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddGlobalExpenseCommand
            {
                ExpenseDate = request.ExpenseDate,
                Category = request.Category,
                Description = request.Description,
                Amount = request.Amount,
            },
            cancellationToken);
        return CreatedAtAction(nameof(GetGlobalExpenses), new { }, result);
    }

    [HttpDelete("global-expenses/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGlobalExpense(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteGlobalExpenseCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}

public record AddGlobalExpenseRequest(DateTimeOffset ExpenseDate, FinanceCategory Category, string Description, decimal Amount);
