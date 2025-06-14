namespace TechTicker.PriceNormalizationService.Messages
{
    /// <summary>
    /// Input event message from Scraper Service containing raw price data
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
    /// Output event message published after price normalization and validation
    /// </summary>
    public class PricePointRecordedEvent
    {
        public Guid CanonicalProductId { get; set; }
        public string SellerName { get; set; } = null!;
        public decimal Price { get; set; }
        public string StockStatus { get; set; } = null!;
        public string SourceUrl { get; set; } = null!;
        public DateTimeOffset Timestamp { get; set; }
        public string? ProductName { get; set; }
        public string? OriginalStockStatus { get; set; }
    }
}
