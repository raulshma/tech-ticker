using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing test execution history
/// </summary>
public interface ITestExecutionHistoryRepository
{
    /// <summary>
    /// Create a new test execution history entry
    /// </summary>
    Task<TestExecutionHistory> CreateAsync(TestExecutionHistory history);

    /// <summary>
    /// Get test execution history with filtering and pagination
    /// </summary>
    Task<(IEnumerable<TestExecutionHistory> History, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? testUrl = null,
        string? profileHash = null,
        Guid? executedBy = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? browserEngine = null,
        string? deviceType = null);

    /// <summary>
    /// Get test execution history by session ID
    /// </summary>
    Task<TestExecutionHistory?> GetBySessionIdAsync(string sessionId);

    /// <summary>
    /// Get test execution trends data
    /// </summary>
    Task<IEnumerable<TestExecutionHistory>> GetTrendsDataAsync(
        string? profileHash = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get popular test URLs with statistics
    /// </summary>
    Task<IEnumerable<(string TestUrl, int TestCount, double SuccessRate, double AverageExecutionTime, DateTime LastTested)>>
        GetPopularTestUrlsAsync(int limit = 10);

    /// <summary>
    /// Get reliability metrics by browser engine
    /// </summary>
    Task<Dictionary<string, double>> GetReliabilityByBrowserAsync();

    /// <summary>
    /// Get flaky tests (URLs with both successful and failed results)
    /// </summary>
    Task<IEnumerable<(string TestUrl, string ProfileHash, int SuccessCount, int FailureCount)>> GetFlakyTestsAsync();

    /// <summary>
    /// Get test execution statistics
    /// </summary>
    Task<(int TotalExecutions, int SuccessfulExecutions, double SuccessRate, double AverageExecutionTime, 
          DateTime? FirstExecution, DateTime? LastExecution, int UniqueUrls, int UniqueProfiles)> 
        GetExecutionStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? profileHash = null);

    /// <summary>
    /// Delete test execution history entries older than specified date
    /// </summary>
    Task<int> DeleteOldHistoryAsync(DateTime beforeDate);

    /// <summary>
    /// Get consistency score for a profile or URL
    /// </summary>
    Task<double> GetConsistencyScoreAsync(string? testUrl = null, string? profileHash = null);
} 