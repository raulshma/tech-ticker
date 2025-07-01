using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing scraper run logs
/// </summary>
public interface IScraperRunLogService
{
    /// <summary>
    /// Get paginated scraper run logs with filtering
    /// </summary>
    Task<Result<PagedResultDto<ScraperRunLogSummaryDto>>> GetPagedLogsAsync(ScraperRunLogFilterDto filter);

    /// <summary>
    /// Get detailed information for a specific run
    /// </summary>
    Task<Result<ScraperRunLogDto>> GetRunByIdAsync(Guid runId);

    /// <summary>
    /// Get logs for a specific mapping
    /// </summary>
    Task<Result<PagedResultDto<ScraperRunLogSummaryDto>>> GetLogsByMappingIdAsync(Guid mappingId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get recent failed runs for dashboard
    /// </summary>
    Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetRecentFailedRunsAsync(int count = 10);

    /// <summary>
    /// Get recent runs for a specific mapping
    /// </summary>
    Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetRecentRunsForMappingAsync(Guid mappingId, int count = 5);

    /// <summary>
    /// Get scraper run statistics
    /// </summary>
    Task<Result<ScraperRunStatisticsDto>> GetStatisticsAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid? mappingId = null);

    /// <summary>
    /// Get retry chain for a specific run
    /// </summary>
    Task<Result<IEnumerable<ScraperRunLogDto>>> GetRetryChainAsync(Guid runId);

    /// <summary>
    /// Get performance metrics by seller
    /// </summary>
    Task<Result<IEnumerable<SellerPerformanceMetricDto>>> GetPerformanceMetricsBySellerAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null);

    /// <summary>
    /// Create a new scraper run log entry
    /// </summary>
    Task<Result<Guid>> CreateRunLogAsync(CreateScraperRunLogDto createDto);

    /// <summary>
    /// Update an existing scraper run log
    /// </summary>
    Task<Result<bool>> UpdateRunLogAsync(Guid runId, UpdateScraperRunLogDto updateDto);

    /// <summary>
    /// Mark a run as completed
    /// </summary>
    Task<Result<bool>> CompleteRunAsync(Guid runId, CompleteScraperRunDto completeDto);

    /// <summary>
    /// Mark a run as failed
    /// </summary>
    Task<Result<bool>> FailRunAsync(Guid runId, FailScraperRunDto failDto);

    /// <summary>
    /// Clean up old log entries
    /// </summary>
    Task<Result<int>> CleanupOldLogsAsync(int daysToKeep = 90);

    /// <summary>
    /// Get runs that are still in progress
    /// </summary>
    Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetInProgressRunsAsync();
}


