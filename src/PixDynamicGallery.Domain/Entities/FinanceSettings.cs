using PixDynamicGallery.Domain.Common;
using PixDynamicGallery.Domain.Exceptions;

namespace PixDynamicGallery.Domain.Entities;

/// <summary>
/// Single-row settings table for finance calculations. Seeded once by an EF Core migration (fixed
/// <see cref="BaseEntity.Id"/>) — there is exactly one row, ever.
/// </summary>
public class FinanceSettings : BaseEntity
{
    public decimal CostPerKm { get; private set; }

    private FinanceSettings()
    {
        // Required by EF Core. Also the only way this type is ever constructed at runtime — the
        // single row is seeded directly by FinanceSettingsConfiguration's HasData (column values,
        // not a constructor call), so there is no public factory here.
    }

    public void UpdateCostPerKm(decimal costPerKm)
    {
        if (costPerKm < 0)
        {
            throw new DomainException("Cost per km cannot be negative.");
        }

        CostPerKm = costPerKm;
    }
}
