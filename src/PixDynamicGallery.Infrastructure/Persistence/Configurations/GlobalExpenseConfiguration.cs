using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence.Configurations;

public class GlobalExpenseConfiguration : IEntityTypeConfiguration<GlobalExpense>
{
    public void Configure(EntityTypeBuilder<GlobalExpense> builder)
    {
        builder.ToTable("GlobalExpenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();

        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.ExpenseDate);
    }
}
