using System.ComponentModel.DataAnnotations;

namespace TechTicker.Shared.Models
{
    /// <summary>
    /// Represents a scraper site configuration for extracting data from e-commerce sites
    /// </summary>
    public class ScraperSiteConfiguration
    {
        [Key]
        public Guid SiteConfigId { get; set; }

        [Required]
        [StringLength(255)]
        public string SiteDomain { get; set; } = null!;

        [Required]
        public string ProductNameSelector { get; set; } = null!;

        [Required]
        public string PriceSelector { get; set; } = null!;

        [Required]
        public string StockSelector { get; set; } = null!;

        public string? SellerNameOnPageSelector { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation property for mappings using this configuration
        public ICollection<ProductSellerMapping> ProductSellerMappings { get; set; } = new List<ProductSellerMapping>();
    }
}
