using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;

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
}
