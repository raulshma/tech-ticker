using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for ScraperRunLog operations
/// </summary>
public interface IScraperRunLogRepository : IRepository<ScraperRunLog>
{
    /// <summary>
    /// Get scraper run logs with pagination and filtering
    /// </summary>
    Task<(IEnumerable<ScraperRunLog> Logs, int TotalCount)> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        Guid? mappingId = null,
        string? status = null,
        string? errorCategory = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sellerName = null);

    /// <summary>
    /// Get logs for a specific mapping with pagination
    /// </summary>
    Task<(IEnumerable<ScraperRunLog> Logs, int TotalCount)> GetByMappingIdAsync(
        Guid mappingId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get recent failed runs for dashboard
    /// </summary>
    Task<IEnumerable<ScraperRunLog>> GetRecentFailedRunsAsync(int count = 10);

    /// <summary>
    /// Get recent runs for a specific mapping
    /// </summary>
    Task<IEnumerable<ScraperRunLog>> GetRecentRunsForMappingAsync(Guid mappingId, int count = 5);

    /// <summary>
    /// Get statistics for scraper runs
    /// </summary>
    Task<ScraperRunStatistics> GetStatisticsAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid? mappingId = null);

    /// <summary>
    /// Get retry chain for a specific run
    /// </summary>
    Task<IEnumerable<ScraperRunLog>> GetRetryChainAsync(Guid runId);

    /// <summary>
    /// Get runs that are still in progress (started but not completed)
    /// </summary>
    Task<IEnumerable<ScraperRunLog>> GetInProgressRunsAsync();

    /// <summary>
    /// Get average response times by seller
    /// </summary>
    Task<IEnumerable<SellerPerformanceMetric>> GetPerformanceMetricsBySellerAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null);

    /// <summary>
    /// Clean up old log entries
    /// </summary>
    Task<int> CleanupOldLogsAsync(DateTimeOffset olderThan);
}

/// <summary>
/// Statistics for scraper runs
/// </summary>
public class ScraperRunStatistics
{
    public int TotalRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public int FailedRuns { get; set; }
    public int InProgressRuns { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? AverageDuration { get; set; }
    public Dictionary<string, int> ErrorCategoryCounts { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}

/// <summary>
/// Performance metrics by seller
/// </summary>
public class SellerPerformanceMetric
{
    public string SellerName { get; set; } = null!;
    public int TotalRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? AverageDuration { get; set; }
}
