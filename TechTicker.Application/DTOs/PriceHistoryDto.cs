namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for PriceHistory responses
/// </summary>
public class PriceHistoryDto
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Price { get; set; }
    public string StockStatus { get; set; } = null!;
    public string SourceUrl { get; set; } = null!;
    public string? ScrapedProductNameOnPage { get; set; }
}

/// <summary>
/// DTO for price history query parameters
/// </summary>
public class PriceHistoryQueryDto
{
    public string? SellerName { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int? Limit { get; set; }
}
