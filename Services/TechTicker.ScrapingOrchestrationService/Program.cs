using TechTicker.ScrapingOrchestrationService.Workers;
using TechTicker.ScrapingOrchestrationService.Services;
using TechTicker.ScrapingOrchestrationService.Data;
using TechTicker.Shared.Extensions;

namespace TechTicker.ScrapingOrchestrationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Add database - use the same connection as ProductSellerMappingService since we need to read from it
        builder.AddNpgsqlDbContext<ScrapingOrchestrationDbContext>("product-seller-mapping");        builder.AddServiceDefaults();// Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Register application services
        builder.Services.AddScoped<IScrapingSchedulerService, ScrapingSchedulerService>();
        builder.Services.AddScoped<IDomainScrapingProfileService, DomainScrapingProfileService>();
        builder.Services.AddScoped<IMessagePublisherService, MessagePublisherService>();// Register the orchestration workers
        builder.Services.AddHostedService<ScrapingOrchestrationWorker>();
        builder.Services.AddHostedService<ScrapingResultConsumerWorker>();        var host = builder.Build();
        
        host.Run();
    }
}
