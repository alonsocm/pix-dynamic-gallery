using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PixDynamicGallery.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add/remove</c> construct an <see cref="ApplicationDbContext"/>
/// directly — without booting the whole API host (DI, SignalR, the watcher's hosted service,
/// etc.) just to scaffold a migration. Only used by the EF Core CLI tooling, never at runtime.
/// Run from the repo root: <c>dotnet ef migrations add NAME --project src/PixDynamicGallery.Infrastructure --startup-project src/PixDynamicGallery.Api</c>.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=pixdynamicgallery;Username=pix;Password=pix";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
