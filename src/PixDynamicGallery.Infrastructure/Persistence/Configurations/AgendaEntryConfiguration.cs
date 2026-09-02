using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class AgendaEntryConfiguration : IEntityTypeConfiguration<AgendaEntry>
{
    public void Configure(EntityTypeBuilder<AgendaEntry> builder)
    {
        builder.ToTable("AgendaEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ClientName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.ContactPhone).HasMaxLength(50);
        builder.Property(a => a.ContactEmail).HasMaxLength(200);
        builder.Property(a => a.EventType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Location).HasMaxLength(300);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.AgreedPrice).HasPrecision(18, 2);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // The agenda screen's main view: bookings ordered by date, often filtered by status.
        builder.HasIndex(a => new { a.EventDate, a.Status });
    }
}
