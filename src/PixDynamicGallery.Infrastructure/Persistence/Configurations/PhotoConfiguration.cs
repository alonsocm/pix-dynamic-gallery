using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("Photos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FileName).HasMaxLength(255).IsRequired();
        builder.Property(p => p.LocalFilePath).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.StorageKey).HasMaxLength(1000);
        builder.Property(p => p.Url).HasMaxLength(1000);
        builder.Property(p => p.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.FailureReason).HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Every write to a given event's watch folder ends up filtered/ordered by these two —
        // covers both "get uploaded photos for the live wall" and "find pending/failed for retry".
        builder.HasIndex(p => new { p.EventId, p.Status, p.UploadedAtUtc });
    }
}
