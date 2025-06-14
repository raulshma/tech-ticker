using TechTicker.ScrapingOrchestrationService.Workers;
using TechTicker.ScrapingOrchestrationService.Services;
using TechTicker.ScrapingOrchestrationService.Data;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.Shared.Extensions;
using TechTicker.ServiceDefaults;

namespace TechTicker.ScrapingOrchestrationService;

public class Program
{
    public static void Main(string[] args)
    {        var builder = Host.CreateApplicationBuilder(args);        // Add database - use separate database for this service's own entities
        builder.AddNpgsqlDbContext<ScrapingOrchestrationDbContext>("scraping-orchestration");
        
        // Add read-only access to ProductSellerMapping database for shared entities
        builder.AddNpgsqlDbContext<ProductSellerMappingDbContext>("product-seller-mapping");
        
        // Add RabbitMQ client
        builder.AddRabbitMQClient("messaging");
        
        builder.AddServiceDefaults();

        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Register application services
        builder.Services.AddScoped<IScrapingSchedulerService, ScrapingSchedulerService>();
        builder.Services.AddScoped<IDomainScrapingProfileService, DomainScrapingProfileService>();
        builder.Services.AddScoped<IMessagePublisherService, MessagePublisherService>();        // Register the orchestration workers
        builder.Services.AddHostedService<ScrapingOrchestrationWorker>();
        builder.Services.AddHostedService<ScrapingResultConsumerWorker>();

        var host = builder.Build();
        
        host.Run();
    }
}
