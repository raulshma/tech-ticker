using TechTicker.Grpc.Contracts.Products;

namespace TechTicker.Grpc.Clients.Products
{
    /// <summary>
    /// Client interface for Product gRPC service operations
    /// </summary>
    public interface IProductGrpcClient
    {
        /// <summary>
        /// Gets a product by ID
        /// </summary>
        Task<ProductResponse?> GetProductAsync(string productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple products by IDs
        /// </summary>
        Task<IEnumerable<ProductResponse>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product exists
        /// </summary>
        Task<bool> ProductExistsAsync(string productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a category by ID or slug
        /// </summary>
        Task<CategoryResponse?> GetCategoryAsync(string categoryIdOrSlug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple categories by IDs
        /// </summary>
        Task<IEnumerable<CategoryResponse>> GetCategoriesAsync(IEnumerable<string> categoryIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a category exists
        /// </summary>
        Task<bool> CategoryExistsAsync(string categoryId, CancellationToken cancellationToken = default);
    }
}
