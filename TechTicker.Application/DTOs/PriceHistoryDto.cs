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

/// <summary>
/// DTO for current price information from a seller
/// </summary>
public class CurrentPriceDto
{
    public string SellerName { get; set; } = null!;
    public decimal Price { get; set; }
    public string StockStatus { get; set; } = null!;
    public string SourceUrl { get; set; } = null!;
    public DateTimeOffset LastUpdated { get; set; }
    public string? ScrapedProductNameOnPage { get; set; }
}

/// <summary>
/// DTO for product with current pricing information
/// </summary>
public class ProductWithCurrentPricesDto : ProductDto
{
    public IEnumerable<CurrentPriceDto> CurrentPrices { get; set; } = new List<CurrentPriceDto>();
    public decimal? LowestCurrentPrice { get; set; }
    public decimal? HighestCurrentPrice { get; set; }
    public int AvailableSellersCount { get; set; }
}
