namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>
/// Global profit/loss overview — /admin/finance. <see cref="TotalRealExpenses"/> is actual cash out
/// (global expenses + paper purchases + non-Photos event expenses); it deliberately excludes the
/// Photos-category event expenses since those are an allocation of money already counted in paper
/// purchases, not new spend (see <see cref="PerEventBreakdownDto.Expense"/> for the per-event view,
/// which does include Photos so each event's own margin is visible).
/// </summary>
public record FinanceDashboardDto
{
    public required decimal TotalIncome { get; init; }

    public required decimal TotalRealExpenses { get; init; }

    public required decimal NetProfit { get; init; }

    public required List<PerEventBreakdownDto> PerEventBreakdown { get; init; }

    public required PaperStockDto PaperStock { get; init; }
}

public record PerEventBreakdownDto
{
    public required Guid EventId { get; init; }

    public required string EventName { get; init; }

    public required decimal Income { get; init; }

    public required decimal Expense { get; init; }

    public required decimal Profit { get; init; }
}
