using System.ComponentModel.DataAnnotations;
using TechTicker.Shared.Constants;

namespace TechTicker.ProductSellerMappingService.DTOs
{
    /// <summary>
    /// DTO for creating a new product seller mapping
    /// </summary>
    public class CreateProductSellerMappingRequest
    {
        [Required]
        public Guid CanonicalProductId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SellerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        [Url]
        public string ExactProductUrl { get; set; } = string.Empty;        public bool IsActiveForScraping { get; set; } = true;

        [MaxLength(50)]
        public string? ScrapingFrequencyOverride { get; set; }

        public Guid? SiteConfigId { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing product seller mapping
    /// </summary>
    public class UpdateProductSellerMappingRequest
    {
        [Required]
        [MaxLength(100)]
        public string SellerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        [Url]
        public string ExactProductUrl { get; set; } = string.Empty;        public bool IsActiveForScraping { get; set; }

        [MaxLength(50)]
        public string? ScrapingFrequencyOverride { get; set; }

        public Guid? SiteConfigId { get; set; }
    }

    /// <summary>
    /// DTO for product seller mapping responses
    /// </summary>
    public class ProductSellerMappingResponse
    {
        public Guid MappingId { get; set; }
        public Guid CanonicalProductId { get; set; }        public string SellerName { get; set; } = string.Empty;
        public string ExactProductUrl { get; set; } = string.Empty;
        public bool IsActiveForScraping { get; set; }
        public string? ScrapingFrequencyOverride { get; set; }
        public Guid? SiteConfigId { get; set; }
        public DateTimeOffset? LastScrapedAt { get; set; }
        public DateTimeOffset? NextScrapeAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for querying product seller mappings
    /// </summary>
    public class ProductSellerMappingQueryRequest
    {
        /// <summary>
        /// Filter by canonical product ID
        /// </summary>
        public Guid? CanonicalProductId { get; set; }

        /// <summary>
        /// Filter by seller name
        /// </summary>
        public string? SellerName { get; set; }

        /// <summary>
        /// Filter by active scraping status
        /// </summary>
        public bool? IsActiveForScraping { get; set; }

        /// <summary>
        /// Search term for URL or seller name
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
