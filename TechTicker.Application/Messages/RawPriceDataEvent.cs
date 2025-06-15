namespace TechTicker.Application.Messages;

/// <summary>
/// Event containing raw scraped price data
/// </summary>
public class RawPriceDataEvent
{
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public decimal ScrapedPrice { get; set; }
    public string ScrapedStockStatus { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? ScrapedProductName { get; set; }
}
