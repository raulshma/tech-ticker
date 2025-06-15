using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Maps canonical products to specific seller URLs and scraping configurations
/// </summary>
public class ProductSellerMapping
{
    [Key]
    public Guid MappingId { get; set; }

    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SellerName { get; set; } = null!;

    [Required]
    [MaxLength(2048)]
    public string ExactProductUrl { get; set; } = null!;

    public bool IsActiveForScraping { get; set; } = true;

    /// <summary>
    /// ISO 8601 Duration format override for scraping frequency (e.g., "PT1H" for 1 hour)
    /// </summary>
    [MaxLength(50)]
    public string? ScrapingFrequencyOverride { get; set; }

    public Guid? SiteConfigId { get; set; }

    public DateTimeOffset? LastScrapedAt { get; set; }

    public DateTimeOffset? NextScrapeAt { get; set; }

    [MaxLength(50)]
    public string? LastScrapeStatus { get; set; }

    [MaxLength(50)]
    public string? LastScrapeErrorCode { get; set; }

    public int ConsecutiveFailureCount { get; set; } = 0;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CanonicalProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(SiteConfigId))]
    public ScraperSiteConfiguration? SiteConfiguration { get; set; }

    public ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
}
