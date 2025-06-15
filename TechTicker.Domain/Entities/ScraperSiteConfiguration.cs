using System.ComponentModel.DataAnnotations;
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

    // Navigation properties
    public ICollection<ProductSellerMapping> ProductSellerMappings { get; set; } = new List<ProductSellerMapping>();

    // Helper property for working with additional headers as dictionary
    public Dictionary<string, string>? AdditionalHeadersDict
    {
        get => string.IsNullOrEmpty(AdditionalHeaders) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, string>>(AdditionalHeaders);
        set => AdditionalHeaders = value == null ? null : JsonSerializer.Serialize(value);
    }
}
