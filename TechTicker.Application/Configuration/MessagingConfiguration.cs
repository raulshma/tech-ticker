namespace TechTicker.Application.Configuration;

/// <summary>
/// Configuration for RabbitMQ messaging topology (exchanges, queues, routing keys)
/// Connection details are handled by Aspire RabbitMQ integration
/// </summary>
public class MessagingConfiguration
{
    public const string SectionName = "Messaging";

    // Exchange names
    public string ScrapingExchange { get; set; } = "techticker.scraping";
    public string AlertsExchange { get; set; } = "techticker.alerts";
    public string PriceDataExchange { get; set; } = "techticker.pricedata";
    public string ProductDiscoveryExchange { get; set; } = "techticker.productdiscovery";

    // Queue names
    public string ScrapeCommandQueue { get; set; } = "scrape.commands";
    public string ScrapingResultQueue { get; set; } = "scraping.results";
    public string RawPriceDataQueue { get; set; } = "pricedata.raw";
    public string PricePointRecordedQueue { get; set; } = "pricedata.recorded";
    public string AlertTriggeredQueue { get; set; } = "alerts.triggered";
    public string ProductDiscoveryEventQueue { get; set; } = "productdiscovery.events";

    // Routing keys
    public string ScrapeCommandRoutingKey { get; set; } = "scrape.command";
    public string ScrapingResultRoutingKey { get; set; } = "scraping.result";
    public string RawPriceDataRoutingKey { get; set; } = "pricedata.raw";
    public string PricePointRecordedRoutingKey { get; set; } = "pricedata.recorded";
    public string AlertTriggeredRoutingKey { get; set; } = "alert.triggered";
    public string ProductDiscoveryEventRoutingKey { get; set; } = "productdiscovery.event";
}
