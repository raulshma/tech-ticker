using TechTicker.PriceNormalizationService.Workers;
using TechTicker.PriceNormalizationService.Services;
using TechTicker.Shared.Extensions;
using TechTicker.ServiceDefaults;

namespace TechTicker.PriceNormalizationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Register application services
        builder.Services.AddScoped<IPriceNormalizationService, Services.PriceNormalizationService>();
        builder.Services.AddScoped<IMessageConsumerService, MessageConsumerService>();
        builder.Services.AddScoped<IMessagePublisherService, MessagePublisherService>();

        // Register the price normalization worker
        builder.Services.AddHostedService<PriceNormalizationWorker>();

        var host = builder.Build();
        
        host.Run();
    }
}
