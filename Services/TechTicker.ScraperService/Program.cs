using TechTicker.ScraperService.Workers;
using TechTicker.ScraperService.Services;
using TechTicker.Shared.Extensions;

namespace TechTicker.ScraperService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddServiceDefaults();

        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Add HTTP client with cookie container for session management
        builder.Services.AddHttpClient<IWebScrapingService, WebScrapingService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            });

        // Register application services
        builder.Services.AddScoped<IWebScrapingService, WebScrapingService>();
        builder.Services.AddScoped<IHtmlParsingService, HtmlParsingService>();
        builder.Services.AddScoped<IMessagePublisherService, MessagePublisherService>();
        builder.Services.AddScoped<IMessageConsumerService, MessageConsumerService>();

        // Register the scraper worker
        builder.Services.AddHostedService<ScraperWorker>();

        var host = builder.Build();
        
        host.Run();
    }
}
