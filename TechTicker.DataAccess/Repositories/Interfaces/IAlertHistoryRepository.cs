using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for AlertHistory entities
/// </summary>
public interface IAlertHistoryRepository : IRepository<AlertHistory>
{
    /// <summary>
    /// Get alert history for a specific user
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get alert history for a specific alert rule
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetByAlertRuleIdAsync(Guid alertRuleId);

    /// <summary>
    /// Get alert history for a specific product
    /// </summary>
    Task<IEnumerable<AlertHistory>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Get paginated alert history with optional filters
    /// </summary>
    Task<(IEnumerable<AlertHistory> Items, int TotalCount)> GetAlertHistoryAsync(
        Guid? userId = null,
        Guid? alertRuleId = null,
        Guid? productId = null,
        string? notificationStatus = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Get alert statistics for a user
    /// </summary>
    Task<AlertHistoryStats> GetUserAlertStatsAsync(Guid userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Get alert statistics for a product
    /// </summary>
    Task<AlertHistoryStats> GetProductAlertStatsAsync(Guid productId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Update notification status for an alert history record
    /// </summary>
    Task UpdateNotificationStatusAsync(Guid alertHistoryId, string status, string? error = null);
}

/// <summary>
/// Statistics for alert history
/// </summary>
public class AlertHistoryStats
{
    public int TotalAlerts { get; set; }
    public int SuccessfulNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public int PendingNotifications { get; set; }
    public double SuccessRate => TotalAlerts > 0 ? (double)SuccessfulNotifications / TotalAlerts * 100 : 0;
    public TimeSpan? AverageNotificationDelay { get; set; }
    public Dictionary<string, int> AlertsByConditionType { get; set; } = new();
    public Dictionary<string, int> AlertsByProduct { get; set; } = new();
}
