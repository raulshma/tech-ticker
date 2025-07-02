using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Main service interface for product comparison functionality
/// </summary>
public interface IProductComparisonService
{
    /// <summary>
    /// Compare two products from the same category with comprehensive analysis
    /// </summary>
    /// <param name="request">Comparison request parameters</param>
    /// <returns>Complete comparison result with analysis</returns>
    Task<Result<ProductComparisonResultDto>> CompareProductsAsync(CompareProductsRequestDto request);
    
    /// <summary>
    /// Validate that two products can be compared (same category, both exist)
    /// </summary>
    /// <param name="productId1">First product ID</param>
    /// <param name="productId2">Second product ID</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateProductsForComparisonAsync(Guid productId1, Guid productId2);
    
    /// <summary>
    /// Get products that can be compared with a given product (same category)
    /// </summary>
    /// <param name="productId">Base product ID</param>
    /// <param name="search">Optional search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of comparable products</returns>
    Task<Result<PagedResponse<ProductDto>>> GetComparableProductsAsync(
        Guid productId, 
        string? search = null, 
        int pageNumber = 1, 
        int pageSize = 10);
}