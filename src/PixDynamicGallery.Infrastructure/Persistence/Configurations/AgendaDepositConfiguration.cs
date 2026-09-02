using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class AgendaDepositConfiguration : IEntityTypeConfiguration<AgendaDeposit>
{
    public void Configure(EntityTypeBuilder<AgendaDeposit> builder)
    {
        builder.ToTable("AgendaDeposits");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasOne<AgendaEntry>()
            .WithMany()
            .HasForeignKey(d => d.AgendaEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // The agenda screen's deposit list for one booking: newest first.
        builder.HasIndex(d => new { d.AgendaEntryId, d.PaymentDate });
    }
}
