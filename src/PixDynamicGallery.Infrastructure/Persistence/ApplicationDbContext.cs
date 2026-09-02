using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Event> Events => Set<Event>();

    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<AgendaEntry> AgendaEntries => Set<AgendaEntry>();

    public DbSet<AgendaDeposit> AgendaDeposits => Set<AgendaDeposit>();

    public DbSet<EventTransaction> EventTransactions => Set<EventTransaction>();

    public DbSet<PaperPurchase> PaperPurchases => Set<PaperPurchase>();

    public DbSet<GlobalExpense> GlobalExpenses => Set<GlobalExpense>();

    public DbSet<FinanceSettings> FinanceSettings => Set<FinanceSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
