using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for ProductSellerMapping operations
/// </summary>
public interface IProductSellerMappingService
{
    Task<Result<ProductSellerMappingDto>> CreateMappingAsync(CreateProductSellerMappingDto createDto);
    Task<Result<IEnumerable<ProductSellerMappingDto>>> GetMappingsByProductIdAsync(Guid productId);
    Task<Result<IEnumerable<ProductSellerMappingDto>>> GetActiveMappingsAsync();
    Task<Result<ProductSellerMappingDto>> UpdateMappingAsync(Guid mappingId, UpdateProductSellerMappingDto updateDto);
    Task<Result> DeleteMappingAsync(Guid mappingId);
}
