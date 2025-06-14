namespace TechTicker.PriceHistoryService.Messages
{
    /// <summary>
    /// Event message received when a price point is recorded after normalization
    /// Consumed from Price Normalization Service
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
