using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for PriceHistory entities
/// </summary>
public interface IPriceHistoryRepository : IRepository<PriceHistory>
{
    Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(
        Guid productId,
        string? sellerName = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int? limit = null);

    Task<PriceHistory?> GetLastPriceAsync(Guid productId, string sellerName);
    Task<PriceHistory?> GetLastRecordedPriceAsync(Guid productId, string sellerName);

    /// <summary>
    /// Get current prices for a product from all sellers (latest price from each seller)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Latest price from each seller for the product</returns>
    Task<IEnumerable<PriceHistory>> GetCurrentPricesAsync(Guid productId);

    /// <summary>
    /// Get current prices for multiple products
    /// </summary>
    /// <param name="productIds">Product IDs</param>
    /// <returns>Latest price from each seller for each product</returns>
    Task<IEnumerable<PriceHistory>> GetCurrentPricesAsync(IEnumerable<Guid> productIds);
}
