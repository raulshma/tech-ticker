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
    public string? DefaultUserAgent { get; set; }
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
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

    public string? DefaultUserAgent { get; set; }

    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public bool IsEnabled { get; set; } = true;
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

    public string? DefaultUserAgent { get; set; }

    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    public bool? IsEnabled { get; set; }
}
