using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class UsbPurchaseConfiguration : IEntityTypeConfiguration<UsbPurchase>
{
    public void Configure(EntityTypeBuilder<UsbPurchase> builder)
    {
        builder.ToTable("UsbPurchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalCost).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(500);

        // CostPerUnit is a computed property (TotalCost / UnitsCount), not a column.
        builder.Ignore(p => p.CostPerUnit);

        builder.HasIndex(p => p.PurchaseDate);
    }
}
