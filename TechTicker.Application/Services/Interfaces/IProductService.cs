using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Product operations
/// </summary>
public interface IProductService
{
    Task<Result<ProductDto>> CreateProductAsync(CreateProductDto createDto);
    Task<Result<PagedResponse<ProductDto>>> GetProductsAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<Result<ProductDto>> GetProductByIdAsync(Guid productId);
    Task<Result<ProductDto>> UpdateProductAsync(Guid productId, UpdateProductDto updateDto);
    Task<Result> DeleteProductAsync(Guid productId);

    /// <summary>
    /// Get products with current pricing information for catalog browsing
    /// </summary>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="search">Optional search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of products with current prices</returns>
    Task<Result<PagedResponse<ProductWithCurrentPricesDto>>> GetProductsWithCurrentPricesAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Get a single product with current pricing information
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Product with current prices</returns>
    Task<Result<ProductWithCurrentPricesDto>> GetProductWithCurrentPricesAsync(Guid productId);
}
