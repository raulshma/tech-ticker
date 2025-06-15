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
}
