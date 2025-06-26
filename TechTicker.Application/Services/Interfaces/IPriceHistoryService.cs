using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for PriceHistory operations
/// </summary>
public interface IPriceHistoryService
{
    Task<Result<IEnumerable<PriceHistoryDto>>> GetPriceHistoryAsync(
        Guid productId,
        PriceHistoryQueryDto queryDto);

    /// <summary>
    /// Get current prices for a product from all sellers
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Current prices from all sellers</returns>
    Task<Result<IEnumerable<CurrentPriceDto>>> GetCurrentPricesAsync(Guid productId);
}
