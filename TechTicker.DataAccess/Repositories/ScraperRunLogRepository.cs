using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for ScraperRunLog operations
/// </summary>
public class ScraperRunLogRepository : Repository<ScraperRunLog>, IScraperRunLogRepository
{
    public ScraperRunLogRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<ScraperRunLog> Logs, int TotalCount)> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        Guid? mappingId = null,
        string? status = null,
        string? errorCategory = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sellerName = null)
    {
        var query = _context.ScraperRunLogs
            .Include(r => r.Mapping)
            .ThenInclude(m => m.Product)
            .AsQueryable();

        // Apply filters
        if (mappingId.HasValue)
            query = query.Where(r => r.MappingId == mappingId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(errorCategory))
            query = query.Where(r => r.ErrorCategory == errorCategory);

        if (dateFrom.HasValue)
            query = query.Where(r => r.StartedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.StartedAt <= dateTo.Value);

        if (!string.IsNullOrEmpty(sellerName))
            query = query.Where(r => r.Mapping.SellerName.Contains(sellerName));

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<(IEnumerable<ScraperRunLog> Logs, int TotalCount)> GetByMappingIdAsync(
        Guid mappingId,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ScraperRunLogs
            .Include(r => r.Mapping)
            .Where(r => r.MappingId == mappingId);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<IEnumerable<ScraperRunLog>> GetRecentFailedRunsAsync(int count = 10)
    {
        return await _context.ScraperRunLogs
            .Include(r => r.Mapping)
            .ThenInclude(m => m.Product)
            .Where(r => r.Status == "FAILED")
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScraperRunLog>> GetRecentRunsForMappingAsync(Guid mappingId, int count = 5)
    {
        return await _context.ScraperRunLogs
            .Where(r => r.MappingId == mappingId)
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<ScraperRunStatistics> GetStatisticsAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid? mappingId = null)
    {
        var query = _context.ScraperRunLogs.AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(r => r.StartedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.StartedAt <= dateTo.Value);

        if (mappingId.HasValue)
            query = query.Where(r => r.MappingId == mappingId.Value);

        var logs = await query.ToListAsync();

        var totalRuns = logs.Count;
        var successfulRuns = logs.Count(r => r.Status == "SUCCESS");
        var failedRuns = logs.Count(r => r.Status == "FAILED");
        var inProgressRuns = logs.Count(r => r.Status == "STARTED");

        var successRate = totalRuns > 0 ? (double)successfulRuns / totalRuns * 100 : 0;

        var completedLogs = logs.Where(r => r.ResponseTime.HasValue).ToList();
        var averageResponseTime = completedLogs.Any() 
            ? TimeSpan.FromMilliseconds(completedLogs.Average(r => r.ResponseTime!.Value.TotalMilliseconds))
            : (TimeSpan?)null;

        var durationLogs = logs.Where(r => r.Duration.HasValue).ToList();
        var averageDuration = durationLogs.Any()
            ? TimeSpan.FromMilliseconds(durationLogs.Average(r => r.Duration!.Value.TotalMilliseconds))
            : (TimeSpan?)null;

        var errorCategoryCounts = logs
            .Where(r => !string.IsNullOrEmpty(r.ErrorCategory))
            .GroupBy(r => r.ErrorCategory!)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusCounts = logs
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ScraperRunStatistics
        {
            TotalRuns = totalRuns,
            SuccessfulRuns = successfulRuns,
            FailedRuns = failedRuns,
            InProgressRuns = inProgressRuns,
            SuccessRate = successRate,
            AverageResponseTime = averageResponseTime,
            AverageDuration = averageDuration,
            ErrorCategoryCounts = errorCategoryCounts,
            StatusCounts = statusCounts
        };
    }

    public async Task<IEnumerable<ScraperRunLog>> GetRetryChainAsync(Guid runId)
    {
        // Get the original run
        var originalRun = await _context.ScraperRunLogs
            .FirstOrDefaultAsync(r => r.RunId == runId);

        if (originalRun == null)
            return Enumerable.Empty<ScraperRunLog>();

        // If this is a retry, get the parent run ID
        var parentRunId = originalRun.ParentRunId ?? runId;

        // Get all runs in the retry chain
        return await _context.ScraperRunLogs
            .Where(r => r.RunId == parentRunId || r.ParentRunId == parentRunId)
            .OrderBy(r => r.AttemptNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScraperRunLog>> GetInProgressRunsAsync()
    {
        return await _context.ScraperRunLogs
            .Include(r => r.Mapping)
            .Where(r => r.Status == "STARTED")
            .OrderBy(r => r.StartedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SellerPerformanceMetric>> GetPerformanceMetricsBySellerAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        var query = _context.ScraperRunLogs
            .Include(r => r.Mapping)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(r => r.StartedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.StartedAt <= dateTo.Value);

        var results = await query
            .GroupBy(r => r.Mapping.SellerName)
            .Select(g => new SellerPerformanceMetric
            {
                SellerName = g.Key,
                TotalRuns = g.Count(),
                SuccessfulRuns = g.Count(r => r.Status == "SUCCESS"),
                SuccessRate = g.Count() > 0 ? (double)g.Count(r => r.Status == "SUCCESS") / g.Count() * 100 : 0,
                AverageResponseTime = g.Where(r => r.ResponseTime.HasValue).Any()
                    ? TimeSpan.FromMilliseconds(g.Where(r => r.ResponseTime.HasValue).Average(r => r.ResponseTime!.Value.TotalMilliseconds))
                    : (TimeSpan?)null,
                AverageDuration = g.Where(r => r.Duration.HasValue).Any()
                    ? TimeSpan.FromMilliseconds(g.Where(r => r.Duration.HasValue).Average(r => r.Duration!.Value.TotalMilliseconds))
                    : (TimeSpan?)null
            })
            .ToListAsync();

        return results;
    }

    public async Task<int> CleanupOldLogsAsync(DateTimeOffset olderThan)
    {
        var oldLogs = await _context.ScraperRunLogs
            .Where(r => r.StartedAt < olderThan)
            .ToListAsync();

        _context.ScraperRunLogs.RemoveRange(oldLogs);
        return oldLogs.Count;
    }
}
