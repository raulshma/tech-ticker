using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for AlertHistory entities
/// </summary>
public class AlertHistoryRepository : Repository<AlertHistory>, IAlertHistoryRepository
{
    public AlertHistoryRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AlertHistory>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(ah => ah.AlertRule)
            .Include(ah => ah.Product)
            .Where(ah => ah.UserId == userId)
            .OrderByDescending(ah => ah.TriggeredAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AlertHistory>> GetByAlertRuleIdAsync(Guid alertRuleId)
    {
        return await _dbSet
            .Include(ah => ah.User)
            .Include(ah => ah.Product)
            .Where(ah => ah.AlertRuleId == alertRuleId)
            .OrderByDescending(ah => ah.TriggeredAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AlertHistory>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(ah => ah.AlertRule)
            .Include(ah => ah.User)
            .Where(ah => ah.CanonicalProductId == productId)
            .OrderByDescending(ah => ah.TriggeredAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<AlertHistory> Items, int TotalCount)> GetAlertHistoryAsync(
        Guid? userId = null,
        Guid? alertRuleId = null,
        Guid? productId = null,
        string? notificationStatus = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _dbSet
            .Include(ah => ah.AlertRule)
            .Include(ah => ah.User)
            .Include(ah => ah.Product)
            .AsQueryable();

        // Apply filters
        if (userId.HasValue)
            query = query.Where(ah => ah.UserId == userId.Value);

        if (alertRuleId.HasValue)
            query = query.Where(ah => ah.AlertRuleId == alertRuleId.Value);

        if (productId.HasValue)
            query = query.Where(ah => ah.CanonicalProductId == productId.Value);

        if (!string.IsNullOrEmpty(notificationStatus))
            query = query.Where(ah => ah.NotificationStatus == notificationStatus);

        if (startDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt <= endDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(ah => ah.TriggeredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AlertHistoryStats> GetUserAlertStatsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var query = _dbSet
            .Include(ah => ah.Product)
            .Where(ah => ah.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt <= endDate.Value);

        var alerts = await query.ToListAsync();

        var stats = new AlertHistoryStats
        {
            TotalAlerts = alerts.Count,
            SuccessfulNotifications = alerts.Count(a => a.NotificationStatus == "SENT"),
            FailedNotifications = alerts.Count(a => a.NotificationStatus == "FAILED"),
            PendingNotifications = alerts.Count(a => a.NotificationStatus == "PENDING"),
            AlertsByConditionType = alerts.GroupBy(a => a.ConditionType)
                .ToDictionary(g => g.Key, g => g.Count()),
            AlertsByProduct = alerts.GroupBy(a => a.Product?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate average notification delay
        var successfulAlerts = alerts.Where(a => a.NotificationDelay.HasValue).ToList();
        if (successfulAlerts.Any())
        {
            var totalTicks = successfulAlerts.Sum(a => a.NotificationDelay!.Value.Ticks);
            stats.AverageNotificationDelay = new TimeSpan(totalTicks / successfulAlerts.Count);
        }

        return stats;
    }

    public async Task<AlertHistoryStats> GetProductAlertStatsAsync(Guid productId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var query = _dbSet
            .Include(ah => ah.Product)
            .Where(ah => ah.CanonicalProductId == productId);

        if (startDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(ah => ah.TriggeredAt <= endDate.Value);

        var alerts = await query.ToListAsync();

        var stats = new AlertHistoryStats
        {
            TotalAlerts = alerts.Count,
            SuccessfulNotifications = alerts.Count(a => a.NotificationStatus == "SENT"),
            FailedNotifications = alerts.Count(a => a.NotificationStatus == "FAILED"),
            PendingNotifications = alerts.Count(a => a.NotificationStatus == "PENDING"),
            AlertsByConditionType = alerts.GroupBy(a => a.ConditionType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate average notification delay
        var successfulAlerts = alerts.Where(a => a.NotificationDelay.HasValue).ToList();
        if (successfulAlerts.Any())
        {
            var totalTicks = successfulAlerts.Sum(a => a.NotificationDelay!.Value.Ticks);
            stats.AverageNotificationDelay = new TimeSpan(totalTicks / successfulAlerts.Count);
        }

        return stats;
    }

    public async Task UpdateNotificationStatusAsync(Guid alertHistoryId, string status, string? error = null)
    {
        var alertHistory = await _dbSet.FindAsync(alertHistoryId);
        if (alertHistory != null)
        {
            alertHistory.NotificationStatus = status;
            alertHistory.NotificationError = error;
            
            if (status == "SENT")
            {
                alertHistory.NotificationSentAt = DateTimeOffset.UtcNow;
            }

            _context.Update(alertHistory);
            await _context.SaveChangesAsync();
        }
    }
}
