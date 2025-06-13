namespace TechTicker.ScrapingOrchestrationService.Messages
{
    /// <summary>
    /// Command message sent to the Scraper Service to initiate scraping of a product page
    /// </summary>
    public class ScrapeProductPageCommand
    {
        public Guid MappingId { get; set; }
        public Guid CanonicalProductId { get; set; }
        public string SellerName { get; set; } = null!;
        public string ExactProductUrl { get; set; } = null!;
        public ScrapingSelectors Selectors { get; set; } = null!;
        public ScrapingProfile ScrapingProfile { get; set; } = null!;
        public DateTimeOffset ScheduledAt { get; set; }
    }

    /// <summary>
    /// CSS/XPath selectors for extracting data from a product page
    /// </summary>
    public class ScrapingSelectors
    {
        public string ProductNameSelector { get; set; } = null!;
        public string PriceSelector { get; set; } = null!;
        public string StockSelector { get; set; } = null!;
        public string? SellerNameOnPageSelector { get; set; }
    }

    /// <summary>
    /// HTTP request configuration to use for scraping
    /// </summary>
    public class ScrapingProfile
    {
        public string UserAgent { get; set; } = null!;
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}
