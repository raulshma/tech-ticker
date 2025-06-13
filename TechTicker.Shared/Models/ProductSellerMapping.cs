using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechTicker.Shared.Models
{
    public class ProductSellerMapping
    {
        [Key]
        public Guid MappingId { get; set; }

        // CanonicalProductId is a logical FK to the Products microservice
        // No direct navigation property as Product is in a separate DbContext/microservice
        public Guid CanonicalProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string SellerName { get; set; } = null!;

        [Required]
        [StringLength(2048)]
        public string ExactProductUrl { get; set; } = null!;

        public bool IsActiveForScraping { get; set; } = true; // DEFAULT TRUE        [StringLength(50)]
        public string? ScrapingFrequencyOverride { get; set; } // Nullable string

        public Guid? SiteConfigId { get; set; } // Foreign key to ScraperSiteConfigurations

        public DateTimeOffset? LastScrapedAt { get; set; } // Nullable TIMESTAMPTZ

        public DateTimeOffset? NextScrapeAt { get; set; } // Nullable TIMESTAMPTZ

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation property for site configuration
        public ScraperSiteConfiguration? SiteConfiguration { get; set; }
    }
}
