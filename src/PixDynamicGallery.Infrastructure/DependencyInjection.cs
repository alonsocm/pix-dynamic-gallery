using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Infrastructure.Files;
using PixDynamicGallery.Infrastructure.Persistence;
using PixDynamicGallery.Infrastructure.Storage;
using PixDynamicGallery.Infrastructure.Watcher;

namespace PixDynamicGallery.Infrastructure;

/// <summary>Composition root extension for the Infrastructure layer — everything that touches the outside world.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        AddStorageProvider(services, configuration);

        services.AddScoped<ILocalCaptureFileReader, LocalCaptureFileReader>();

        services.Configure<SparkboothWatcherOptions>(configuration.GetSection(SparkboothWatcherOptions.SectionName));
        services.AddHostedService<SparkboothWatcherService>();

        return services;
    }

    private static void AddStorageProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<StorageProvider>($"{StorageOptions.SectionName}:{nameof(StorageOptions.Provider)}");

        switch (provider)
        {
            case StorageProvider.AzureBlob:
                services.AddSingleton(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
                    return new BlobServiceClient(options.AzureBlob.ConnectionString);
                });
                services.AddScoped<IStorageService, AzureBlobStorageService>();
                break;

            case StorageProvider.AwsS3:
            default:
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value.AwsS3;

                    var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region) };

                    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                    {
                        // Local/self-hosted S3-compatible endpoint (e.g. MinIO in docker-compose).
                        config.ServiceURL = options.ServiceUrl;
                        config.ForcePathStyle = true;
                    }

                    if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey))
                    {
                        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                        return new AmazonS3Client(credentials, config);
                    }

                    // No explicit keys configured: fall back to the default AWS credential chain
                    // (IAM role, environment variables, shared profile) — the recommended path in production.
                    return new AmazonS3Client(config);
                });
                services.AddScoped<IStorageService, S3StorageService>();
                break;
        }
    }
}
