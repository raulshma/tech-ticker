using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Configuration for scraping specific e-commerce sites
/// </summary>
public class ScraperSiteConfiguration
{
    [Key]
    public Guid SiteConfigId { get; set; }

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

    /// <summary>
    /// CSS selector for extracting product images
    /// </summary>
    public string? ImageSelector { get; set; }

    public string? DefaultUserAgent { get; set; }

    /// <summary>
    /// Additional headers as JSON key-value pairs
    /// </summary>
    public string? AdditionalHeaders { get; set; }

    public bool IsEnabled { get; set; } = true;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    public bool RequiresBrowserAutomation { get; set; } = false;

    /// <summary>
    /// Browser automation profile as JSON (serialized BrowserAutomationProfile)
    /// </summary>
    public string? BrowserAutomationProfile { get; set; }

    /// <summary>
    /// CSS selector for specification tables on the product page
    /// </summary>
    public string? SpecificationTableSelector { get; set; }

    /// <summary>
    /// CSS selector for the container holding specification tables (optional)
    /// </summary>
    public string? SpecificationContainerSelector { get; set; }

    /// <summary>
    /// Whether specification scraping is enabled for this site
    /// </summary>
    public bool EnableSpecificationScraping { get; set; } = false;

    /// <summary>
    /// Specification parsing options as JSON
    /// </summary>
    public string? SpecificationParsingOptions { get; set; }

    // Navigation properties
    public ICollection<ProductSellerMapping> ProductSellerMappings { get; set; } = new List<ProductSellerMapping>();

    // Helper property for working with additional headers as dictionary
    [NotMapped]
    public Dictionary<string, string>? AdditionalHeadersDict
    {
        get => string.IsNullOrEmpty(AdditionalHeaders) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, string>>(AdditionalHeaders);
        set => AdditionalHeaders = value == null ? null : JsonSerializer.Serialize(value);
    }
}
