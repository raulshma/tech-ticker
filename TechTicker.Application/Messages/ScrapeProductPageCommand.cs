namespace TechTicker.Application.Messages;

/// <summary>
/// Command to scrape a product page
/// </summary>
public class ScrapeProductPageCommand
{
    public Guid MappingId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public string ExactProductUrl { get; set; } = null!;
    public ScrapingSelectors Selectors { get; set; } = null!;
    public ScrapingProfile ScrapingProfile { get; set; } = null!;
}

public class ScrapingSelectors
{
    public string ProductNameSelector { get; set; } = null!;
    public string PriceSelector { get; set; } = null!;
    public string StockSelector { get; set; } = null!;
    public string? SellerNameOnPageSelector { get; set; }
}

public class ScrapingProfile
{
    public string UserAgent { get; set; } = null!;
    public Dictionary<string, string>? Headers { get; set; }
}
