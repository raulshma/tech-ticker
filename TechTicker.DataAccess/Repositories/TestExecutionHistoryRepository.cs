using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for managing test execution history
/// </summary>
public class TestExecutionHistoryRepository : ITestExecutionHistoryRepository
{
    private readonly TechTickerDbContext _context;

    public TestExecutionHistoryRepository(TechTickerDbContext context)
    {
        _context = context;
    }

    public async Task<TestExecutionHistory> CreateAsync(TestExecutionHistory history)
    {
        history.Id = Guid.NewGuid();
        
        _context.TestExecutionHistory.Add(history);
        await _context.SaveChangesAsync();
        
        return history;
    }

    public async Task<(IEnumerable<TestExecutionHistory> History, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? testUrl = null,
        string? profileHash = null,
        Guid? executedBy = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? browserEngine = null,
        string? deviceType = null)
    {
        var query = _context.TestExecutionHistory
            .Include(h => h.ExecutedByUser)
            .Include(h => h.SavedTestResult)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(testUrl))
        {
            query = query.Where(h => h.TestUrl.Contains(testUrl));
        }

        if (!string.IsNullOrEmpty(profileHash))
        {
            query = query.Where(h => h.ProfileHash == profileHash);
        }

        if (executedBy.HasValue)
        {
            query = query.Where(h => h.ExecutedBy == executedBy.Value);
        }

        if (success.HasValue)
        {
            query = query.Where(h => h.Success == success.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(browserEngine))
        {
            query = query.Where(h => h.BrowserEngine == browserEngine);
        }

        if (!string.IsNullOrEmpty(deviceType))
        {
            query = query.Where(h => h.DeviceType == deviceType);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var results = await query
            .OrderByDescending(h => h.ExecutedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (results, totalCount);
    }

    public async Task<TestExecutionHistory?> GetBySessionIdAsync(string sessionId)
    {
        return await _context.TestExecutionHistory
            .Include(h => h.ExecutedByUser)
            .Include(h => h.SavedTestResult)
            .FirstOrDefaultAsync(h => h.SessionId == sessionId);
    }

    public async Task<IEnumerable<TestExecutionHistory>> GetTrendsDataAsync(
        string? profileHash = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.TestExecutionHistory.AsQueryable();

        if (!string.IsNullOrEmpty(profileHash))
        {
            query = query.Where(h => h.ProfileHash == profileHash);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt <= toDate.Value);
        }

        return await query
            .OrderBy(h => h.ExecutedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<(string TestUrl, int TestCount, double SuccessRate, double AverageExecutionTime, DateTime LastTested)>>
        GetPopularTestUrlsAsync(int limit = 10)
    {
        return await _context.TestExecutionHistory
            .GroupBy(h => h.TestUrl)
            .Select(g => new
            {
                TestUrl = g.Key,
                TestCount = g.Count(),
                SuccessRate = g.Count() > 0 ? (double)g.Count(h => h.Success) / g.Count() * 100 : 0,
                AverageExecutionTime = g.Count() > 0 ? g.Average(h => (double)h.Duration) : 0,
                LastTested = g.Max(h => h.ExecutedAt)
            })
            .OrderByDescending(x => x.TestCount)
            .Take(limit)
            .ToListAsync()
            .ContinueWith(task => task.Result.Select(x => 
                (x.TestUrl, x.TestCount, x.SuccessRate, x.AverageExecutionTime, x.LastTested)));
    }

    public async Task<Dictionary<string, double>> GetReliabilityByBrowserAsync()
    {
        return await _context.TestExecutionHistory
            .GroupBy(h => h.BrowserEngine)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count() > 0 ? (double)g.Count(h => h.Success) / g.Count() * 100 : 0);
    }

    public async Task<IEnumerable<(string TestUrl, string ProfileHash, int SuccessCount, int FailureCount)>> GetFlakyTestsAsync()
    {
        return await _context.TestExecutionHistory
            .GroupBy(h => new { h.TestUrl, h.ProfileHash })
            .Where(g => g.Any(h => h.Success) && g.Any(h => !h.Success))
            .Select(g => new
            {
                TestUrl = g.Key.TestUrl,
                ProfileHash = g.Key.ProfileHash,
                SuccessCount = g.Count(h => h.Success),
                FailureCount = g.Count(h => !h.Success)
            })
            .ToListAsync()
            .ContinueWith(task => task.Result.Select(x => 
                (x.TestUrl, x.ProfileHash, x.SuccessCount, x.FailureCount)));
    }

    public async Task<(int TotalExecutions, int SuccessfulExecutions, double SuccessRate, double AverageExecutionTime, 
        DateTime? FirstExecution, DateTime? LastExecution, int UniqueUrls, int UniqueProfiles)> 
        GetExecutionStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? profileHash = null)
    {
        var query = _context.TestExecutionHistory.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.ExecutedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(profileHash))
        {
            query = query.Where(h => h.ProfileHash == profileHash);
        }

        var executions = await query.ToListAsync();

        var totalExecutions = executions.Count;
        var successfulExecutions = executions.Count(h => h.Success);
        var successRate = totalExecutions > 0 ? (double)successfulExecutions / totalExecutions * 100 : 0;
        var averageExecutionTime = totalExecutions > 0 ? executions.Average(h => (double)h.Duration) : 0;
        var firstExecution = executions.Count > 0 ? executions.Min(h => h.ExecutedAt) : (DateTime?)null;
        var lastExecution = executions.Count > 0 ? executions.Max(h => h.ExecutedAt) : (DateTime?)null;
        var uniqueUrls = executions.Select(h => h.TestUrl).Distinct().Count();
        var uniqueProfiles = executions.Select(h => h.ProfileHash).Distinct().Count();

        return (totalExecutions, successfulExecutions, successRate, averageExecutionTime, 
                firstExecution, lastExecution, uniqueUrls, uniqueProfiles);
    }

    public async Task<int> DeleteOldHistoryAsync(DateTime beforeDate)
    {
        var oldEntries = await _context.TestExecutionHistory
            .Where(h => h.ExecutedAt < beforeDate)
            .ToListAsync();

        _context.TestExecutionHistory.RemoveRange(oldEntries);
        await _context.SaveChangesAsync();

        return oldEntries.Count;
    }

    public async Task<double> GetConsistencyScoreAsync(string? testUrl = null, string? profileHash = null)
    {
        var query = _context.TestExecutionHistory.AsQueryable();

        if (!string.IsNullOrEmpty(testUrl))
        {
            query = query.Where(h => h.TestUrl == testUrl);
        }

        if (!string.IsNullOrEmpty(profileHash))
        {
            query = query.Where(h => h.ProfileHash == profileHash);
        }

        var executions = await query.ToListAsync();

        if (executions.Count < 2)
        {
            return 100; // Perfect consistency if less than 2 executions
        }

        var durations = executions.Select(h => (double)h.Duration).ToList();
        var average = durations.Average();
        var variance = durations.Sum(d => Math.Pow(d - average, 2)) / durations.Count;
        var standardDeviation = Math.Sqrt(variance);

        // Consistency score based on coefficient of variation
        var coefficientOfVariation = average > 0 ? standardDeviation / average : 0;
        return Math.Max(0, 100 - (coefficientOfVariation * 100));
    }
} 