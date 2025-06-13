using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TechTicker.Shared.Constants;

namespace TechTicker.ProductService.DTOs
{
    /// <summary>
    /// DTO for creating a new product
    /// </summary>
    public class CreateProductRequest
    {
        [Required]
        [MaxLength(TechTickerConstants.Products.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(TechTickerConstants.Products.MaxManufacturerLength)]
        public string? Manufacturer { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxModelNumberLength)]
        public string? ModelNumber { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxSkuLength)]
        public string? SKU { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxDescriptionLength)]
        public string? Description { get; set; }

        public JsonElement? Specifications { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating an existing product
    /// </summary>
    public class UpdateProductRequest
    {
        [Required]
        [MaxLength(TechTickerConstants.Products.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(TechTickerConstants.Products.MaxManufacturerLength)]
        public string? Manufacturer { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxModelNumberLength)]
        public string? ModelNumber { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxSkuLength)]
        public string? SKU { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [MaxLength(TechTickerConstants.Products.MaxDescriptionLength)]
        public string? Description { get; set; }

        public JsonElement? Specifications { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for product responses
    /// </summary>
    public class ProductResponse
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Manufacturer { get; set; }
        public string? ModelNumber { get; set; }
        public string? SKU { get; set; }
        public Guid CategoryId { get; set; }
        public CategoryResponse? Category { get; set; }
        public string? Description { get; set; }
        public JsonElement? Specifications { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }    /// <summary>
    /// DTO for category responses (simplified for product context)
    /// </summary>
    public class CategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int? ProductCount { get; set; }
    }

    /// <summary>
    /// DTO for product list queries
    /// </summary>
    public class ProductQueryRequest
    {
        /// <summary>
        /// Category ID or slug to filter by
        /// </summary>
        public string? CategoryId { get; set; }

        /// <summary>
        /// Search term for product name, manufacturer, or model number
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
