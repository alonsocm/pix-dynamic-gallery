using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class FinanceSettingsConfiguration : IEntityTypeConfiguration<FinanceSettings>
{
    /// <summary>Fixed id of the single settings row — seeded once by migration, never re-created.</summary>
    public static readonly Guid SingletonId = new("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<FinanceSettings> builder)
    {
        builder.ToTable("FinanceSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CostPerKm).HasPrecision(18, 4).IsRequired();

        // MXN 3.00/km is a reasonable starting default for a small hatchback around Mexico's
        // average gas price — the operator can change it any time from /admin/finance.
        //
        // HasData seeds by column values, not by running the entity's field initializers — so
        // CreatedAtUtc (BaseEntity's `= DateTimeOffset.UtcNow` default) must be pinned explicitly
        // here. Leaving it to FinanceSettings.Seed's default would re-evaluate UtcNow every time
        // EF rebuilds the model, which EF then flags as "the model changes every build" and
        // refuses to migrate.
        builder.HasData(new
        {
            Id = SingletonId,
            CreatedAtUtc = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero),
            CostPerKm = 3.00m,
        });
    }
}
