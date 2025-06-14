namespace TechTicker.MigrationService;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using OpenTelemetry.Trace;
using TechTicker.PriceHistoryService.Data;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductService.Data;
using TechTicker.ScrapingOrchestrationService.Data;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();

            await RunMigrationAsync(scope, cancellationToken);
            await SeedDataAsync(scope, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var productDbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var strategy = productDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await productDbContext.Database.MigrateAsync(cancellationToken);
        });

        var productSellerMappingDbContext = scope.ServiceProvider.GetRequiredService<ProductSellerMappingDbContext>();
        strategy = productSellerMappingDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await productSellerMappingDbContext.Database.MigrateAsync(cancellationToken);
        });        var priceHistoryDbContext = scope.ServiceProvider.GetRequiredService<PriceHistoryDbContext>();
        strategy = priceHistoryDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await priceHistoryDbContext.Database.MigrateAsync(cancellationToken);
        });

        var scrapingOrchestrationDbContext = scope.ServiceProvider.GetRequiredService<ScrapingOrchestrationDbContext>();
        strategy = scrapingOrchestrationDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await scrapingOrchestrationDbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static Task SeedDataAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        // for future
        return Task.CompletedTask;
    }
}