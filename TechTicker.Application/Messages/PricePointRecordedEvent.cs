namespace TechTicker.Application.Messages;

/// <summary>
/// Event published when a price point is recorded after normalization
/// </summary>
public class PricePointRecordedEvent
{
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public decimal Price { get; set; }
    public string StockStatus { get; set; } = null!;
    public string SourceUrl { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
}
