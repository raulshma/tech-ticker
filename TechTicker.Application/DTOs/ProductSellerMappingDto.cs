using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for ProductSellerMapping responses
/// </summary>
public class ProductSellerMappingDto
{
    public Guid MappingId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string SellerName { get; set; } = null!;
    public string ExactProductUrl { get; set; } = null!;
    public bool IsActiveForScraping { get; set; }
    public string? ScrapingFrequencyOverride { get; set; }
    public Guid? SiteConfigId { get; set; }
    public DateTimeOffset? LastScrapedAt { get; set; }
    public DateTimeOffset? NextScrapeAt { get; set; }
    public string? LastScrapeStatus { get; set; }
    public string? LastScrapeErrorCode { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public ProductDto? Product { get; set; }
    public ScraperSiteConfigurationDto? SiteConfiguration { get; set; }
}

/// <summary>
/// DTO for creating a new product seller mapping
/// </summary>
public class CreateProductSellerMappingDto
{
    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SellerName { get; set; } = null!;

    [Required]
    [MaxLength(2048)]
    [Url]
    public string ExactProductUrl { get; set; } = null!;

    public bool IsActiveForScraping { get; set; } = true;

    [MaxLength(50)]
    public string? ScrapingFrequencyOverride { get; set; }

    public Guid? SiteConfigId { get; set; }
}

/// <summary>
/// DTO for updating a product seller mapping
/// </summary>
public class UpdateProductSellerMappingDto
{
    [MaxLength(100)]
    public string? SellerName { get; set; }

    [MaxLength(2048)]
    [Url]
    public string? ExactProductUrl { get; set; }

    public bool? IsActiveForScraping { get; set; }

    [MaxLength(50)]
    public string? ScrapingFrequencyOverride { get; set; }

    public Guid? SiteConfigId { get; set; }
}
