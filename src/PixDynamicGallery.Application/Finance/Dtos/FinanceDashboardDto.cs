namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>
/// Global profit/loss overview — /admin/finance. <see cref="TotalRealExpenses"/> is actual cash out
/// (global expenses + paper purchases + non-Photos event expenses); it deliberately excludes the
/// Photos-category event expenses since those are an allocation of money already counted in paper
/// purchases, not new spend (see <see cref="PerEventBreakdownDto.Expense"/> for the per-event view,
/// which does include Photos so each event's own margin is visible).
///
/// <see cref="TotalIncome"/> also includes <see cref="PendingDepositsTotal"/> — agenda deposits not
/// yet transferred to an event's own income (see <see cref="AgendaDeposits"/>). This studio only
/// adds a booking to the agenda once a deposit has actually been received, so an untransferred
/// deposit is real money in hand, not speculative income — it just doesn't have a technical Event
/// to be an <c>EventTransaction</c> on yet.
/// </summary>
public record FinanceDashboardDto
{
    public required decimal TotalIncome { get; init; }

    public required decimal TotalRealExpenses { get; init; }

    public required decimal NetProfit { get; init; }

    public required List<PerEventBreakdownDto> PerEventBreakdown { get; init; }

    public required PaperStockDto PaperStock { get; init; }

    /// <summary>Sum of every agenda deposit not yet transferred to an event — already folded into <see cref="TotalIncome"/>, broken out here for visibility.</summary>
    public required decimal PendingDepositsTotal { get; init; }

    public required List<PendingAgendaDepositDto> AgendaDeposits { get; init; }
}

public record PerEventBreakdownDto
{
    public required Guid EventId { get; init; }

    public required string EventName { get; init; }

    public required decimal Income { get; init; }

    public required decimal Expense { get; init; }

    public required decimal Profit { get; init; }
}

/// <summary>One agenda booking's not-yet-transferred deposits, aggregated for the dashboard.</summary>
public record PendingAgendaDepositDto
{
    public required Guid AgendaEntryId { get; init; }

    public required string ClientName { get; init; }

    public required DateTimeOffset EventDate { get; init; }

    public required decimal Total { get; init; }
}
