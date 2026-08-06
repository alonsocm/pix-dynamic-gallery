using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations on startup and, in Development, seeds a demo event so the
/// Angular kiosk/wall/guest pages have something to point at without manual setup. Called once
/// from the API's <c>Program.cs</c> — kept out of <c>DependencyInjection.AddInfrastructure</c>
/// since "run migrations now" is a startup action, not a service registration.
/// </summary>
public class ApplicationDbContextInitializer(
    ApplicationDbContext context,
    ILogger<ApplicationDbContextInitializer> logger)
{
    public async Task InitializeAsync()
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        if (await context.Events.AnyAsync())
        {
            return;
        }

        logger.LogInformation("No events found — seeding a demo event for local development.");

        var demoEvent = Event.Create(
            name: "Demo Event",
            slug: "demo",
            watchFolderPath: @"C:\SparkboothPhotos\Demo",
            guestBaseUrl: "http://localhost:4200");

        context.Events.Add(demoEvent);
        await context.SaveChangesAsync();
    }
}
