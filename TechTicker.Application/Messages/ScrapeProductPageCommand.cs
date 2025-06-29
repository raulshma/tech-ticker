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
    public bool RequiresBrowserAutomation { get; set; } = false;
    public BrowserAutomationProfile? BrowserAutomationProfile { get; set; }
}

public class ScrapingSelectors
{
    public string ProductNameSelector { get; set; } = null!;
    public string PriceSelector { get; set; } = null!;
    public string StockSelector { get; set; } = null!;
    public string? SellerNameOnPageSelector { get; set; }
    public string? ImageSelector { get; set; }
}

public class ScrapingProfile
{
    public string UserAgent { get; set; } = null!;
    public Dictionary<string, string>? Headers { get; set; }
}

public class BrowserAutomationProfile
{
    public string? PreferredBrowser { get; set; } // e.g., "chromium", "firefox", "webkit"
    public int? WaitTimeMs { get; set; }
    public List<BrowserAutomationAction>? Actions { get; set; } // e.g., scroll, click, etc.
    public int? TimeoutSeconds { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? ProxyServer { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
}

public class BrowserAutomationAction
{
    /// <summary>
    /// Supported actions: scroll, click, waitForSelector, type, wait, screenshot, evaluate, hover, selectOption, setValue, waitForTimeout
    /// </summary>
    public string ActionType { get; set; } = null!; // e.g., "scroll", "click", "type", etc.
    public string? Selector { get; set; }
    public int? Repeat { get; set; }
    public int? DelayMs { get; set; }
    public string? Value { get; set; } // For actions that require a value (e.g., type, evaluate, screenshot path, selectOption, setValue)
}
