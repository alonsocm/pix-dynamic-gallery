using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Event> Events => Set<Event>();

    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
