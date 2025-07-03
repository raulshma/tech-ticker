using System.ComponentModel.DataAnnotations;
using TechTicker.Shared.Utilities.Html;

namespace TechTicker.Application.DTOs;

/// <summary>
/// Command for scraping a product page with specification support
/// </summary>
public class ScrapeProductPageCommand
{
    public Guid MappingId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public string ExactProductUrl { get; set; } = null!;
    public ScrapingSelectorsDto Selectors { get; set; } = null!;
    public ScrapingProfileDto ScrapingProfile { get; set; } = null!;
    public bool RequiresBrowserAutomation { get; set; } = false;
    public BrowserAutomationProfileDto? BrowserAutomationProfile { get; set; }
    public bool ScrapeSpecifications { get; set; } = false;
}

/// <summary>
/// Selectors for scraping data from product pages
/// </summary>
public class ScrapingSelectorsDto
{
    public string ProductNameSelector { get; set; } = null!;
    public string PriceSelector { get; set; } = null!;
    public string StockSelector { get; set; } = null!;
    public string? SellerNameOnPageSelector { get; set; }
    public string? ImageSelector { get; set; }
    
    // Specification scraping selectors
    public string? SpecificationTableSelector { get; set; }
    public string? SpecificationContainerSelector { get; set; }
    public SpecificationParsingOptions? SpecificationOptions { get; set; }
}

/// <summary>
/// Scraping profile configuration
/// </summary>
public class ScrapingProfileDto
{
    public string UserAgent { get; set; } = null!;
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Browser automation profile configuration
/// </summary>
public class BrowserAutomationProfileDto
{
    public string? PreferredBrowser { get; set; }
    public int? WaitTimeMs { get; set; }
    public List<BrowserAutomationActionDto>? Actions { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? ProxyServer { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
}

/// <summary>
/// Browser automation action configuration
/// </summary>
public class BrowserAutomationActionDto
{
    public string ActionType { get; set; } = null!;
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public int? DelayMs { get; set; }
    public int? Repeat { get; set; }
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>
/// Result of a web scraping operation
/// </summary>
public class ScrapingResult
{
    public bool IsSuccess { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public string? StockStatus { get; set; }
    public string? SellerNameOnPage { get; set; }
    public DateTimeOffset ScrapedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    // Image-related properties
    public string? PrimaryImageUrl { get; set; }
    public List<string> AdditionalImageUrls { get; set; } = new();
    public List<string> OriginalImageUrls { get; set; } = new();
    
    // Specification scraping result
    public ProductSpecificationResult? Specifications { get; set; }
}

/// <summary>
/// Result of product specification scraping
/// </summary>
public class ProductSpecificationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Specifications { get; set; } = new();
    public ParseMetadata Metadata { get; set; } = new();
    public QualityMetrics Quality { get; set; } = new();
    public long ParsingTimeMs { get; set; }
    public Dictionary<string, Domain.Entities.Canonical.NormalizedSpecificationValue> NormalizedSpecifications { get; set; } = new();
    public Dictionary<string, string> UncategorizedSpecifications { get; set; } = new();
}

/// <summary>
/// DTO for creating a scraper run log entry
/// </summary>
public class CreateScraperRunLogDto
{
    [Required]
    public Guid MappingId { get; set; }

    [Required]
    [MaxLength(2048)]
    public string TargetUrl { get; set; } = null!;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public ScrapingSelectorsDto? Selectors { get; set; }

    public int AttemptNumber { get; set; } = 1;

    public Guid? ParentRunId { get; set; }

    [MaxLength(1000)]
    public string? DebugNotes { get; set; }
}

/// <summary>
/// DTO for updating a scraper run log with results
/// </summary>
public class CompleteScraperRunDto
{
    [MaxLength(500)]
    public string? ExtractedProductName { get; set; }

    public decimal? ExtractedPrice { get; set; }

    [MaxLength(100)]
    public string? ExtractedStockStatus { get; set; }

    [MaxLength(200)]
    public string? ExtractedSellerName { get; set; }

    // Image extraction results
    [MaxLength(2048)]
    public string? ExtractedPrimaryImageUrl { get; set; }

    public List<string>? ExtractedAdditionalImageUrls { get; set; }

    public List<string>? ExtractedOriginalImageUrls { get; set; }

    public int? ImageProcessingCount { get; set; }

    public int? ImageUploadCount { get; set; }

    [MaxLength(500)]
    public string? ImageScrapingError { get; set; }

    // Performance metrics
    public TimeSpan? ResponseTime { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }

    [MaxLength(2000)]
    public string? RawHtmlSnippet { get; set; }

    [MaxLength(1000)]
    public string? DebugNotes { get; set; }

    public string? ProxyUsed { get; set; }

    public Guid? ProxyId { get; set; }
    
    // Specification scraping results
    public string? SpecificationData { get; set; } // JSON
    public string? SpecificationMetadata { get; set; } // JSON
    public int? SpecificationCount { get; set; }
    public string? SpecificationParsingStrategy { get; set; }
    public double? SpecificationQualityScore { get; set; }
    public long? SpecificationParsingTime { get; set; }
    public string? SpecificationError { get; set; }
}

/// <summary>
/// DTO for updating a scraper run log with specification results
/// </summary>
public class UpdateScraperRunLogDto
{
    public int? HttpStatusCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public long? ResponseSizeBytes { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }
    public string? RawHtmlSnippet { get; set; }
    public string? ProxyUsed { get; set; }
    public Guid? ProxyId { get; set; }
    
    public string? SpecificationData { get; set; } // JSON
    public string? SpecificationMetadata { get; set; } // JSON
    public int? SpecificationCount { get; set; }
    public string? SpecificationParsingStrategy { get; set; }
    public double? SpecificationQualityScore { get; set; }
    public long? SpecificationParsingTime { get; set; }
    public string? SpecificationError { get; set; }
    
    [MaxLength(1000)]
    public string? DebugNotes { get; set; }
}

/// <summary>
/// DTO for marking a scraper run as failed
/// </summary>
public class FailScraperRunDto
{
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? ErrorCode { get; set; }

    public string? ErrorStackTrace { get; set; }

    [MaxLength(50)]
    public string? ErrorCategory { get; set; }

    public int? HttpStatusCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }

    [MaxLength(2000)]
    public string? RawHtmlSnippet { get; set; }

    [MaxLength(1000)]
    public string? DebugNotes { get; set; }

    public string? ProxyUsed { get; set; }

    public Guid? ProxyId { get; set; }
    
    // Specification error handling
    public string? SpecificationError { get; set; }
} 