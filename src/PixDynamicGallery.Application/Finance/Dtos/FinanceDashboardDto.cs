namespace PixDynamicGallery.Application.Finance.Dtos;

/// <summary>
/// Global balance and reports — /admin/finance. Stock/inventory lives in a separate module
/// (/admin/inventory, see PaperStockDto/UsbStockDto in Application.Inventory.Dtos) — this DTO is
/// money only.
///
/// <see cref="TotalRealExpenses"/> is actual cash out (global expenses + paper purchases + USB
/// purchases + event expenses that aren't Photos or Usb); it deliberately excludes the Photos- and
/// Usb-category event expenses since those are allocations of money already counted in paper/USB
/// purchases, not new spend (see <see cref="PerEventBreakdownDto.Expense"/> for the per-event view,
/// which does include them so each event's own margin is visible).
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

    /// <summary>Sum of every agenda deposit not yet transferred to an event — already folded into <see cref="TotalIncome"/>, broken out here for visibility.</summary>
    public required decimal PendingDepositsTotal { get; init; }

    public required List<PendingAgendaDepositDto> AgendaDeposits { get; init; }

    /// <summary>Income (transactions + still-pending agenda deposits) grouped by the month the money actually came in. Only months with data are included.</summary>
    public required List<MonthlyAmountDto> IncomeByMonth { get; init; }

    /// <summary>Non-cancelled agenda bookings grouped by their event month — the technical Event carries no date of its own, so this reads from the agenda.</summary>
    public required List<MonthlyCountDto> EventsByMonth { get; init; }
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

public record MonthlyAmountDto
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required decimal Total { get; init; }
}

public record MonthlyCountDto
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Count { get; init; }
}
