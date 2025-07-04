using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for ScraperSiteConfiguration responses
/// </summary>
public class ScraperSiteConfigurationDto
{
    public Guid SiteConfigId { get; set; }
    public string SiteDomain { get; set; } = null!;
    public string ProductNameSelector { get; set; } = null!;
    public string PriceSelector { get; set; } = null!;
    public string StockSelector { get; set; } = null!;
    public string? SellerNameOnPageSelector { get; set; }
    public string? ImageSelector { get; set; }
    public string? DefaultUserAgent { get; set; }
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool RequiresBrowserAutomation { get; set; }
    public string? BrowserAutomationProfile { get; set; }
    
    // Specification scraping properties
    public string? SpecificationTableSelector { get; set; }
    public string? SpecificationContainerSelector { get; set; }
    public bool EnableSpecificationScraping { get; set; }
    public SpecificationParsingOptions? SpecificationOptions { get; set; }
}

/// <summary>
/// Configuration options for specification parsing
/// </summary>
public class SpecificationParsingOptions
{
    public bool EnableCaching { get; set; } = true;
    public bool ThrowOnError { get; set; } = false;
    public int MaxCacheEntries { get; set; } = 1000;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromHours(24);
    public string PreferredVendor { get; set; } = "Generic";
}

/// <summary>
/// DTO for creating a new scraper site configuration
/// </summary>
public class CreateScraperSiteConfigurationDto
{
    [Required]
    [MaxLength(255)]
    public string SiteDomain { get; set; } = null!;

    [Required]
    public string ProductNameSelector { get; set; } = null!;

    [Required]
    public string PriceSelector { get; set; } = null!;

    [Required]
    public string StockSelector { get; set; } = null!;

    public string? SellerNameOnPageSelector { get; set; }

    public string? ImageSelector { get; set; }

    public string? DefaultUserAgent { get; set; }

    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool RequiresBrowserAutomation { get; set; } = false;
    public string? BrowserAutomationProfile { get; set; }
    
    // Specification scraping properties
    public string? SpecificationTableSelector { get; set; }
    public string? SpecificationContainerSelector { get; set; }
    public bool EnableSpecificationScraping { get; set; } = false;
    public SpecificationParsingOptions? SpecificationOptions { get; set; }
}

/// <summary>
/// DTO for updating a scraper site configuration
/// </summary>
public class UpdateScraperSiteConfigurationDto
{
    [MaxLength(255)]
    public string? SiteDomain { get; set; }

    public string? ProductNameSelector { get; set; }

    public string? PriceSelector { get; set; }

    public string? StockSelector { get; set; }

    public string? SellerNameOnPageSelector { get; set; }

    public string? ImageSelector { get; set; }

    public string? DefaultUserAgent { get; set; }

    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public bool? IsEnabled { get; set; }

    public bool? RequiresBrowserAutomation { get; set; }
    public string? BrowserAutomationProfile { get; set; }
    
    // Specification scraping properties
    public string? SpecificationTableSelector { get; set; }
    public string? SpecificationContainerSelector { get; set; }
    public bool? EnableSpecificationScraping { get; set; }
    public SpecificationParsingOptions? SpecificationOptions { get; set; }
}
