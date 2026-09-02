using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class PaperPurchaseConfiguration : IEntityTypeConfiguration<PaperPurchase>
{
    public void Configure(EntityTypeBuilder<PaperPurchase> builder)
    {
        builder.ToTable("PaperPurchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalCost).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(500);

        // CostPerSheet is a computed property (TotalCost / SheetsCount), not a column.
        builder.Ignore(p => p.CostPerSheet);

        builder.HasIndex(p => p.PurchaseDate);
    }
}
