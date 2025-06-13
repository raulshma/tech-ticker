using TechTicker.ProductService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductService.Services
{
    /// <summary>
    /// Interface for category service operations
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Creates a new category
        /// </summary>
        Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request);

        /// <summary>
        /// Gets a category by ID or slug
        /// </summary>
        Task<Result<CategoryResponse>> GetCategoryByIdOrSlugAsync(string categoryIdOrSlug, bool includeProductCount = true);

        /// <summary>
        /// Gets a paginated list of categories with optional filtering
        /// </summary>
        Task<Result<PagedResponse<CategoryResponse>>> GetCategoriesAsync(CategoryQueryRequest query);

        /// <summary>
        /// Updates an existing category
        /// </summary>
        Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request);

        /// <summary>
        /// Deletes a category
        /// </summary>
        Task<Result> DeleteCategoryAsync(Guid categoryId);

        /// <summary>
        /// Checks if a category exists by ID
        /// </summary>
        Task<bool> CategoryExistsAsync(Guid categoryId);

        /// <summary>
        /// Checks if a category slug is already in use (excluding the specified category ID)
        /// </summary>
        Task<bool> IsSlugInUseAsync(string slug, Guid? excludeCategoryId = null);

        /// <summary>
        /// Checks if a category name is already in use (excluding the specified category ID)
        /// </summary>
        Task<bool> IsNameInUseAsync(string name, Guid? excludeCategoryId = null);

        /// <summary>
        /// Gets the product count for a category
        /// </summary>
        Task<int> GetProductCountAsync(Guid categoryId);
    }
}
