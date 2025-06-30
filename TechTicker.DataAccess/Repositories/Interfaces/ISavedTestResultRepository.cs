using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing saved test results
/// </summary>
public interface ISavedTestResultRepository
{
    /// <summary>
    /// Get all saved test results with pagination and filtering
    /// </summary>
    Task<(IEnumerable<SavedTestResult> Results, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        List<string>? tags = null,
        Guid? createdBy = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Get a saved test result by ID
    /// </summary>
    Task<SavedTestResult?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get saved test results by profile hash
    /// </summary>
    Task<IEnumerable<SavedTestResult>> GetByProfileHashAsync(string profileHash);

    /// <summary>
    /// Get saved test results by test URL
    /// </summary>
    Task<IEnumerable<SavedTestResult>> GetByTestUrlAsync(string testUrl);

    /// <summary>
    /// Create a new saved test result
    /// </summary>
    Task<SavedTestResult> CreateAsync(SavedTestResult savedTestResult);

    /// <summary>
    /// Update an existing saved test result
    /// </summary>
    Task<SavedTestResult> UpdateAsync(SavedTestResult savedTestResult);

    /// <summary>
    /// Delete a saved test result
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Bulk delete saved test results
    /// </summary>
    Task<int> BulkDeleteAsync(List<Guid> ids);

    /// <summary>
    /// Get all unique tags
    /// </summary>
    Task<List<string>> GetAllTagsAsync();

    /// <summary>
    /// Get test statistics
    /// </summary>
    Task<(int TotalTests, int SuccessfulTests, int FailedTests, double AverageExecutionTime)> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? profileHash = null);

    /// <summary>
    /// Check if a test result exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Get saved test results for comparison
    /// </summary>
    Task<IEnumerable<SavedTestResult>> GetForComparisonAsync(List<Guid> ids);
} 