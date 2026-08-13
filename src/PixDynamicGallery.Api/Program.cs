using System.Reflection;
using PixDynamicGallery.Api.Auth;
using PixDynamicGallery.Api.Hubs;
using PixDynamicGallery.Api.Middleware;
using PixDynamicGallery.Application;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Infrastructure;
using PixDynamicGallery.Infrastructure.Persistence;
using Serilog;

const string CorsPolicyName = "PixDynamicGallery.Clients";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // --- Clean Architecture layers -------------------------------------------------------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- Presentation (this project) ------------------------------------------------------
    builder.Services.AddControllers();
    builder.Services.AddScoped<IPhotoNotifier, PhotoNotifier>();

    builder.Services.AddSignalR();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Pix Dynamic Gallery API",
            Version = "v1",
            Description =
                "Real-time companion API for Sparkbooth photobooth events: watches the local capture " +
                "folder, uploads to cloud storage, and broadcasts new photos over SignalR to the kiosk " +
                "screen and guest PWA.",
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Admin-only endpoints (event list/toggle, bulk photo delete) — see AdminAuthAttribute. Empty
    // password (the default) means the check is skipped entirely, so local dev/docker-compose
    // needs zero setup.
    builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
    if (string.IsNullOrEmpty(builder.Configuration[$"{AdminOptions.SectionName}:Password"]))
    {
        Log.Warning("Admin:Password is not set — the admin area (event list, active toggle, photo delete) is unprotected.");
    }

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            // SignalR needs credentialed CORS (AllowCredentials), which is incompatible with
            // AllowAnyOrigin — so Production sticks to an explicit, configuration-driven allow-list.
            // Development instead accepts any origin (still credentialed, via SetIsOriginAllowed)
            // so local tools work without config changes: file:// pages send `Origin: null`, and
            // ad-hoc dev servers (Angular's, tools/signalr-test-client.html, ...) land on random
            // ports — an allow-list would need editing every time.
            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    var app = builder.Build();

    // --- Startup: apply migrations (+ seed demo data in Development) ----------------------
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        await initializer.InitializeAsync();

        if (app.Environment.IsDevelopment())
        {
            await initializer.SeedAsync();
        }
    }

    // --- HTTP request pipeline --------------------------------------------------------------
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pix Dynamic Gallery API v1"));
    }

    app.UseHttpsRedirection();

    app.UseCors(CorsPolicyName);

    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<EventHub>("/hubs/event");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown by `dotnet ef` design-time tooling, which builds the host
    // just far enough to resolve DbContext and then intentionally stops it — not a real failure.
    Log.Fatal(ex, "Pix Dynamic Gallery API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposes the generated <c>Program</c> class for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
