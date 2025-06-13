using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductSellerMappingService.Services
{
    /// <summary>
    /// Interface for product seller mapping service operations
    /// </summary>
    public interface IProductSellerMappingService
    {
        /// <summary>
        /// Creates a new product seller mapping
        /// </summary>
        Task<Result<ProductSellerMappingResponse>> CreateMappingAsync(CreateProductSellerMappingRequest request);

        /// <summary>
        /// Gets a paginated list of mappings with optional filtering
        /// </summary>
        Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetMappingsAsync(ProductSellerMappingQueryRequest query);

        /// <summary>
        /// Gets mappings for a specific canonical product
        /// </summary>
        Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetMappingsByProductIdAsync(Guid canonicalProductId, int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Gets all active mappings (used by orchestrator for scraping)
        /// </summary>
        Task<Result<PagedResponse<ProductSellerMappingResponse>>> GetActiveMappingsAsync(int pageNumber = 1, int pageSize = 100);

        /// <summary>
        /// Gets a mapping by ID
        /// </summary>
        Task<Result<ProductSellerMappingResponse>> GetMappingByIdAsync(Guid mappingId);

        /// <summary>
        /// Updates an existing mapping
        /// </summary>
        Task<Result<ProductSellerMappingResponse>> UpdateMappingAsync(Guid mappingId, UpdateProductSellerMappingRequest request);

        /// <summary>
        /// Deletes a mapping
        /// </summary>
        Task<Result> DeleteMappingAsync(Guid mappingId);

        /// <summary>
        /// Checks if a mapping exists
        /// </summary>
        Task<bool> MappingExistsAsync(Guid mappingId);

        /// <summary>
        /// Checks if a mapping already exists for the given product and seller
        /// </summary>
        Task<bool> IsDuplicateMappingAsync(Guid canonicalProductId, string sellerName, Guid? excludeMappingId = null);
    }
}
