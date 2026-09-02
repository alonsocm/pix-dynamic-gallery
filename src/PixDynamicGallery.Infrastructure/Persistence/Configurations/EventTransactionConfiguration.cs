using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class EventTransactionConfiguration : IEntityTypeConfiguration<EventTransaction>
{
    public void Configure(EntityTypeBuilder<EventTransaction> builder)
    {
        builder.ToTable("EventTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.CostPerPhotoSnapshot).HasPrecision(18, 4);
        builder.Property(t => t.DistanceKm).HasPrecision(18, 2);
        builder.Property(t => t.CostPerKmSnapshot).HasPrecision(18, 4);
        builder.Property(t => t.CostPerUsbSnapshot).HasPrecision(18, 4);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(t => t.Event)
            .WithMany()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // The per-event finance screen: transactions for one event, newest first.
        builder.HasIndex(t => new { t.EventId, t.TransactionDate });

        // PaperStockCalculator sums PhotoCount across every Photos-category row for the global stock figure.
        builder.HasIndex(t => t.Category);
    }
}
