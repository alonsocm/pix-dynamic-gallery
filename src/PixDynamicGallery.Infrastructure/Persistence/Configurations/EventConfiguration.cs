using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        builder.Property(e => e.WatchFolderPath).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.GuestBaseUrl).HasMaxLength(500).IsRequired();

        builder.HasIndex(e => e.Slug).IsUnique();

        builder.HasMany(e => e.Photos)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event.Photos is exposed as a read-only projection over a private backing field
        // (encapsulated collection, per DDD) — tell EF to read/write through the field directly.
        builder.Metadata.FindNavigation(nameof(Event.Photos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
