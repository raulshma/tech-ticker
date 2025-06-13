namespace TechTicker.ScraperService.Messages
{
    /// <summary>
    /// Event message published when raw price data is successfully scraped
    /// </summary>
    public class RawPriceDataEvent
    {
        public Guid CanonicalProductId { get; set; }
        public string SellerName { get; set; } = null!;
        public decimal ScrapedPrice { get; set; }
        public string ScrapedStockStatus { get; set; } = null!;
        public DateTimeOffset Timestamp { get; set; }
        public string SourceUrl { get; set; } = null!;
        public string ScrapedProductName { get; set; } = null!;
    }

    /// <summary>
    /// Event message published after every scraping attempt (success or failure)
    /// </summary>
    public class ScrapingResultEvent
    {
        public Guid MappingId { get; set; }
        public bool WasSuccessful { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public int? HttpStatusCode { get; set; }
    }
}
