namespace TechTicker.MigrationService;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using TechTicker.PriceHistoryService.Data;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductService.Data;
using TechTicker.ScrapingOrchestrationService.Data;
using TechTicker.UserService.Data;

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

        var userDbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        strategy = userDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await userDbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();

        // Seed test users for development
        await SeedTestUsersAsync(scope, logger, cancellationToken);
    }

    private static async Task SeedTestUsersAsync(IServiceScope scope, ILogger<Worker> logger, CancellationToken cancellationToken)
    {
        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        // Check if we're in development environment
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            logger.LogInformation("Skipping test user seeding - not in development environment");
            return;
        }

        // Test users to create
        var testUsers = new[]
        {
            new { Email = "test@techticker.com", Password = "Test123!", FirstName = "Test", LastName = "User" },
            new { Email = "admin@techticker.com", Password = "Admin123!", FirstName = "Admin", LastName = "User" },
            new { Email = "demo@techticker.com", Password = "Demo123!", FirstName = "Demo", LastName = "User" }
        };

        foreach (var testUser in testUsers)
        {
            // Check if user already exists
            if (await userContext.Users.AnyAsync(u => u.Email == testUser.Email, cancellationToken))
            {
                logger.LogInformation("Test user {Email} already exists, skipping", testUser.Email);
                continue;
            }

            // Create new test user
            var user = new TechTicker.Shared.Models.User
            {
                UserId = Guid.NewGuid(),
                Email = testUser.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(testUser.Password),
                FirstName = testUser.FirstName,
                LastName = testUser.LastName,
                IsActive = true,
                EmailConfirmed = true, // Auto-confirm for test users
                EmailConfirmedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            userContext.Users.Add(user);
            logger.LogInformation("Created test user: {Email}", testUser.Email);
        }

        await userContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Test user seeding completed");
    }
}