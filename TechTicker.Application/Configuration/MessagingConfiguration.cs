namespace TechTicker.Application.Configuration;

/// <summary>
/// Configuration for RabbitMQ messaging
/// </summary>
public class MessagingConfiguration
{
    public const string SectionName = "Messaging";

    public string ConnectionString { get; set; } = "amqp://localhost:5672";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    // Exchange names
    public string ScrapingExchange { get; set; } = "techticker.scraping";
    public string AlertsExchange { get; set; } = "techticker.alerts";
    public string PriceDataExchange { get; set; } = "techticker.pricedata";

    // Queue names
    public string ScrapeCommandQueue { get; set; } = "scrape.commands";
    public string ScrapingResultQueue { get; set; } = "scraping.results";
    public string RawPriceDataQueue { get; set; } = "pricedata.raw";
    public string PricePointRecordedQueue { get; set; } = "pricedata.recorded";
    public string AlertTriggeredQueue { get; set; } = "alerts.triggered";

    // Routing keys
    public string ScrapeCommandRoutingKey { get; set; } = "scrape.command";
    public string ScrapingResultRoutingKey { get; set; } = "scraping.result";
    public string RawPriceDataRoutingKey { get; set; } = "pricedata.raw";
    public string PricePointRecordedRoutingKey { get; set; } = "pricedata.recorded";
    public string AlertTriggeredRoutingKey { get; set; } = "alert.triggered";
}
