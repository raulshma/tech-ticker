namespace TechTicker.Application.Messages;

/// <summary>
/// Event containing raw scraped price data
/// </summary>
public class RawPriceDataEvent
{
    public Guid MappingId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public decimal ScrapedPrice { get; set; }
    public string ScrapedStockStatus { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? ScrapedProductName { get; set; }

    // Image-related properties
    public string? PrimaryImageUrl { get; set; }
    public List<string> AdditionalImageUrls { get; set; } = new();
    public List<string> OriginalImageUrls { get; set; } = new();
}
