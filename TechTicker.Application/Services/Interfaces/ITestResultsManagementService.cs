using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for managing saved test results, history, and comparison
/// </summary>
public interface ITestResultsManagementService
{
    /// <summary>
    /// Save test results for future reference
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="name">Name for the saved result</param>
    /// <param name="description">Optional description</param>
    /// <param name="tags">Optional tags</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved result ID</returns>
    Task<Result<string>> SaveTestResultsAsync(
        string sessionId,
        string name,
        string? description = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get saved test results with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="tags">Optional tag filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated saved test results</returns>
    Task<Result<PagedResponse<SavedTestResultDto>>> GetSavedTestResultsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific saved test result by ID
    /// </summary>
    /// <param name="savedResultId">Saved result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved test result details</returns>
    Task<Result<SavedTestResultDetailDto>> GetSavedTestResultAsync(
        string savedResultId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a saved test result
    /// </summary>
    /// <param name="savedResultId">Saved result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> DeleteSavedTestResultAsync(
        string savedResultId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare two test results
    /// </summary>
    /// <param name="firstResultId">First test result ID</param>
    /// <param name="secondResultId">Second test result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison result</returns>
    Task<Result<TestResultComparisonDto>> CompareTestResultsAsync(
        string firstResultId,
        string secondResultId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get test execution trends and statistics
    /// </summary>
    /// <param name="profileId">Optional profile ID filter</param>
    /// <param name="fromDate">Optional date range start</param>
    /// <param name="toDate">Optional date range end</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test trends and statistics</returns>
    Task<Result<TestExecutionTrendsDto>> GetTestExecutionTrendsAsync(
        string? profileId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export test results in various formats
    /// </summary>
    /// <param name="savedResultId">Saved result ID</param>
    /// <param name="format">Export format (JSON, CSV, PDF)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported data as byte array</returns>
    Task<Result<ExportedTestResultDto>> ExportTestResultAsync(
        string savedResultId,
        TestResultExportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get test history for a specific URL or profile
    /// </summary>
    /// <param name="testUrl">Optional test URL filter</param>
    /// <param name="profileHash">Optional profile hash filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test history</returns>
    Task<Result<List<TestHistoryEntryDto>>> GetTestHistoryAsync(
        string? testUrl = null,
        string? profileHash = null,
        int limit = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Export format for test results
/// </summary>
public enum TestResultExportFormat
{
    Json,
    Csv,
    Pdf
} 