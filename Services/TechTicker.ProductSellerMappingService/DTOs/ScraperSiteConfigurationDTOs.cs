using System.ComponentModel.DataAnnotations;

namespace TechTicker.ProductSellerMappingService.DTOs
{
    /// <summary>
    /// DTO for creating a new scraper site configuration
    /// </summary>
    public class CreateScraperSiteConfigurationRequest
    {
        [Required]
        [MaxLength(255)]
        public string SiteDomain { get; set; } = string.Empty;

        [Required]
        public string ProductNameSelector { get; set; } = string.Empty;

        [Required]
        public string PriceSelector { get; set; } = string.Empty;

        [Required]
        public string StockSelector { get; set; } = string.Empty;

        public string? SellerNameOnPageSelector { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing scraper site configuration
    /// </summary>
    public class UpdateScraperSiteConfigurationRequest
    {
        [Required]
        [MaxLength(255)]
        public string SiteDomain { get; set; } = string.Empty;

        [Required]
        public string ProductNameSelector { get; set; } = string.Empty;

        [Required]
        public string PriceSelector { get; set; } = string.Empty;

        [Required]
        public string StockSelector { get; set; } = string.Empty;

        public string? SellerNameOnPageSelector { get; set; }
    }

    /// <summary>
    /// DTO for scraper site configuration responses
    /// </summary>
    public class ScraperSiteConfigurationResponse
    {
        public Guid SiteConfigId { get; set; }
        public string SiteDomain { get; set; } = string.Empty;
        public string ProductNameSelector { get; set; } = string.Empty;
        public string PriceSelector { get; set; } = string.Empty;
        public string StockSelector { get; set; } = string.Empty;
        public string? SellerNameOnPageSelector { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for querying scraper site configurations
    /// </summary>
    public class ScraperSiteConfigurationQueryRequest
    {
        /// <summary>
        /// Filter by site domain
        /// </summary>
        public string? SiteDomain { get; set; }

        /// <summary>
        /// Search term for domain
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 50;
    }
}
