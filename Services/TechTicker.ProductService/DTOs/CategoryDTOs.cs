using System.ComponentModel.DataAnnotations;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductService.DTOs
{
    /// <summary>
    /// DTO for creating a new category
    /// </summary>
    public class CreateCategoryRequest
    {
        [Required]
        [MaxLength(TechTickerConstants.Categories.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(TechTickerConstants.Categories.MaxSlugLength)]
        public string? Slug { get; set; }

        [MaxLength(TechTickerConstants.Categories.MaxDescriptionLength)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets the slug, generating it from the name if not provided
        /// </summary>
        public string GetSlug()
        {
            return !string.IsNullOrEmpty(Slug) ? Slug : StringUtilities.ToSlug(Name);
        }
    }

    /// <summary>
    /// DTO for updating an existing category
    /// </summary>
    public class UpdateCategoryRequest
    {
        [Required]
        [MaxLength(TechTickerConstants.Categories.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(TechTickerConstants.Categories.MaxSlugLength)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(TechTickerConstants.Categories.MaxDescriptionLength)]
        public string? Description { get; set; }
    }    /// <summary>
    /// DTO for category list queries
    /// </summary>
    public class CategoryQueryRequest
    {
        /// <summary>
        /// Search term for category name or description
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Include product count in response
        /// </summary>
        public bool IncludeProductCount { get; set; } = true;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 50; // Categories are typically fewer, so larger page size
    }
}
