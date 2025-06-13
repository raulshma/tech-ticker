using TechTicker.ProductService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductService.Services
{
    /// <summary>
    /// Interface for product service operations
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Creates a new product
        /// </summary>
        Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request);

        /// <summary>
        /// Gets a product by ID
        /// </summary>
        Task<Result<ProductResponse>> GetProductByIdAsync(Guid productId);

        /// <summary>
        /// Gets a paginated list of products with optional filtering
        /// </summary>
        Task<Result<PagedResponse<ProductResponse>>> GetProductsAsync(ProductQueryRequest query);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        Task<Result<ProductResponse>> UpdateProductAsync(Guid productId, UpdateProductRequest request);

        /// <summary>
        /// Deletes a product
        /// </summary>
        Task<Result> DeleteProductAsync(Guid productId);

        /// <summary>
        /// Checks if a product exists
        /// </summary>
        Task<bool> ProductExistsAsync(Guid productId);

        /// <summary>
        /// Checks if a SKU is already in use (excluding the specified product ID)
        /// </summary>
        Task<bool> IsSkuInUseAsync(string sku, Guid? excludeProductId = null);

        /// <summary>
        /// Validates if a category exists
        /// </summary>
        Task<bool> CategoryExistsAsync(Guid categoryId);
    }
}
