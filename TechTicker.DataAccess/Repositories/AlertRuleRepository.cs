using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for AlertRule entities
/// </summary>
public class AlertRuleRepository : Repository<AlertRule>, IAlertRuleRepository
{
    public AlertRuleRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AlertRule>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(ar => ar.Product)
            .ThenInclude(p => p.Category)
            .Where(ar => ar.UserId == userId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AlertRule>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(ar => ar.User)
            .Where(ar => ar.CanonicalProductId == productId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AlertRule>> GetActiveAlertsForProductAsync(Guid productId, string? sellerName = null)
    {
        var query = _dbSet
            .Include(ar => ar.User)
            .Include(ar => ar.Product)
            .ThenInclude(p => p.Category)
            .Where(ar => ar.CanonicalProductId == productId && ar.IsActive);

        if (!string.IsNullOrWhiteSpace(sellerName))
        {
            query = query.Where(ar => ar.SpecificSellerName == null || ar.SpecificSellerName == sellerName);
        }

        return await query.ToListAsync();
    }

    public async Task<(IEnumerable<AlertRule> Items, int TotalCount)> GetAllAlertsAsync(
        Guid? userId = null,
        Guid? productId = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _dbSet
            .Include(ar => ar.User)
            .Include(ar => ar.Product)
            .ThenInclude(p => p.Category)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(ar => ar.UserId == userId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(ar => ar.CanonicalProductId == productId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(ar => ar.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
