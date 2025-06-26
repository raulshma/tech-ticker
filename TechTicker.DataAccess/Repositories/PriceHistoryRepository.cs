using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for PriceHistory entities
/// </summary>
public class PriceHistoryRepository : Repository<PriceHistory>, IPriceHistoryRepository
{
    public PriceHistoryRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(
        Guid productId,
        string? sellerName = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int? limit = null)
    {
        var query = _dbSet.Where(ph => ph.CanonicalProductId == productId);

        if (!string.IsNullOrWhiteSpace(sellerName))
        {
            query = query.Where(ph => ph.SellerName == sellerName);
        }

        if (startDate.HasValue)
        {
            query = query.Where(ph => ph.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(ph => ph.Timestamp <= endDate.Value);
        }

        query = query.OrderByDescending(ph => ph.Timestamp);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<PriceHistory?> GetLastPriceAsync(Guid productId, string sellerName)
    {
        return await _dbSet
            .Where(ph => ph.CanonicalProductId == productId && ph.SellerName == sellerName)
            .OrderByDescending(ph => ph.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<PriceHistory?> GetLastRecordedPriceAsync(Guid productId, string sellerName)
    {
        return await _dbSet
            .Where(ph => ph.CanonicalProductId == productId && ph.SellerName == sellerName)
            .OrderByDescending(ph => ph.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PriceHistory>> GetCurrentPricesAsync(Guid productId)
    {
        // Get the latest price from each seller for the product
        var latestPrices = await _dbSet
            .Where(ph => ph.CanonicalProductId == productId)
            .GroupBy(ph => ph.SellerName)
            .Select(g => g.OrderByDescending(ph => ph.Timestamp).First())
            .ToListAsync();

        return latestPrices;
    }

    public async Task<IEnumerable<PriceHistory>> GetCurrentPricesAsync(IEnumerable<Guid> productIds)
    {
        var productIdsList = productIds.ToList();

        // Get the latest price from each seller for each product
        var latestPrices = await _dbSet
            .Where(ph => productIdsList.Contains(ph.CanonicalProductId))
            .GroupBy(ph => new { ph.CanonicalProductId, ph.SellerName })
            .Select(g => g.OrderByDescending(ph => ph.Timestamp).First())
            .ToListAsync();

        return latestPrices;
    }
}
