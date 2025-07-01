using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// Controller for test results management, comparison, and analysis
/// </summary>
[ApiController]
[Route("api/test-results")]
public class TestResultsManagementController : BaseApiController
{
    private readonly ITestResultsManagementService _testResultsService;
    private readonly ILogger<TestResultsManagementController> _logger;

    public TestResultsManagementController(
        ITestResultsManagementService testResultsService,
        ILogger<TestResultsManagementController> logger)
    {
        _testResultsService = testResultsService;
        _logger = logger;
    }

    /// <summary>
    /// Save test results from a session
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="request">Save request with name and details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved result ID</returns>
    [HttpPost("sessions/{sessionId}/save")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<string>>> SaveTestResults(
        string sessionId,
        [FromBody] SaveTestResultRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving test results for session {SessionId} with name {Name}", 
                sessionId, request.Name);

            var result = await _testResultsService.SaveTestResultsAsync(
                sessionId, 
                request.Name, 
                request.Description, 
                request.Tags, 
                cancellationToken);
            
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving test results for session {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get saved test results with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="tags">Optional tag filter (comma-separated)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated saved test results</returns>
    [HttpGet("saved")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<PagedResponse<SavedTestResultDto>>> GetSavedTestResults(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting saved test results - Page: {Page}, Size: {Size}, Search: {Search}, Tags: {Tags}", 
                pageNumber, pageSize, searchTerm, tags);

            var tagsList = !string.IsNullOrEmpty(tags) 
                ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() 
                : null;

            var result = await _testResultsService.GetSavedTestResultsAsync(
                pageNumber, 
                pageSize, 
                searchTerm, 
                tagsList, 
                cancellationToken);
            
            return HandlePagedResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved test results");
            return StatusCode(500, PagedResponse<SavedTestResultDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get a specific saved test result by ID
    /// </summary>
    /// <param name="savedResultId">Saved result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed saved test result</returns>
    [HttpGet("saved/{savedResultId}")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<SavedTestResultDetailDto>>> GetSavedTestResult(
        string savedResultId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting saved test result {SavedResultId}", savedResultId);

            var result = await _testResultsService.GetSavedTestResultAsync(savedResultId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved test result {SavedResultId}", savedResultId);
            return StatusCode(500, ApiResponse<SavedTestResultDetailDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Delete a saved test result
    /// </summary>
    /// <param name="savedResultId">Saved result ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("saved/{savedResultId}")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSavedTestResult(
        string savedResultId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting saved test result {SavedResultId}", savedResultId);

            var result = await _testResultsService.DeleteSavedTestResultAsync(savedResultId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved test result {SavedResultId}", savedResultId);
            return StatusCode(500, ApiResponse<bool>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Compare two test results
    /// </summary>
    /// <param name="request">Comparison request with result IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed comparison result</returns>
    [HttpPost("compare")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<TestResultComparisonDto>>> CompareTestResults(
        [FromBody] CompareTestResultsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Comparing test results {FirstResultId} and {SecondResultId}", 
                request.FirstResultId, request.SecondResultId);

            var result = await _testResultsService.CompareTestResultsAsync(
                request.FirstResultId, 
                request.SecondResultId, 
                cancellationToken);
            
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing test results {FirstResultId} and {SecondResultId}", 
                request.FirstResultId, request.SecondResultId);
            return StatusCode(500, ApiResponse<TestResultComparisonDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get test execution trends and statistics
    /// </summary>
    /// <param name="profileId">Optional profile ID filter</param>
    /// <param name="fromDate">Optional date range start (ISO 8601)</param>
    /// <param name="toDate">Optional date range end (ISO 8601)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test trends and statistics</returns>
    [HttpGet("trends")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<TestExecutionTrendsDto>>> GetTestExecutionTrends(
        [FromQuery] string? profileId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting test execution trends for profile {ProfileId} from {FromDate} to {ToDate}", 
                profileId, fromDate, toDate);

            var result = await _testResultsService.GetTestExecutionTrendsAsync(
                profileId, 
                fromDate, 
                toDate, 
                cancellationToken);
            
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test execution trends");
            return StatusCode(500, ApiResponse<TestExecutionTrendsDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Export test result in various formats
    /// </summary>
    /// <param name="savedResultId">Saved result ID to export</param>
    /// <param name="format">Export format (json, csv, pdf)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file data</returns>
    [HttpGet("saved/{savedResultId}/export")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<IActionResult> ExportTestResult(
        string savedResultId,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting test result {SavedResultId} in format {Format}", savedResultId, format);

            if (!Enum.TryParse<TestResultExportFormat>(format, true, out var exportFormat))
            {
                return BadRequest(ApiResponse<string>.FailureResult("Invalid export format", 400));
            }

            var result = await _testResultsService.ExportTestResultAsync(savedResultId, exportFormat, cancellationToken);
            
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(result.ErrorMessage ?? "Export failed", 404));
            }

            var exportData = result.Data;
            return File(exportData.Data, exportData.ContentType, exportData.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting test result {SavedResultId}", savedResultId);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get test history for a specific URL or profile
    /// </summary>
    /// <param name="testUrl">Optional test URL filter</param>
    /// <param name="profileHash">Optional profile hash filter</param>
    /// <param name="limit">Maximum number of results (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test history entries</returns>
    [HttpGet("history")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<List<TestHistoryEntryDto>>>> GetTestHistory(
        [FromQuery] string? testUrl = null,
        [FromQuery] string? profileHash = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting test history for URL {TestUrl}, Profile {ProfileHash}, Limit {Limit}", 
                testUrl, profileHash, limit);

            var result = await _testResultsService.GetTestHistoryAsync(testUrl, profileHash, limit, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test history");
            return StatusCode(500, ApiResponse<List<TestHistoryEntryDto>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get available tags for filtering saved test results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available tags</returns>
    [HttpGet("tags")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetAvailableTags(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting available tags for test results");

            var result = await _testResultsService.GetAvailableTagsAsync(cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tags");
            return StatusCode(500, ApiResponse<List<string>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get test statistics summary
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall test statistics</returns>
    [HttpGet("statistics")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<TestStatistics>>> GetTestStatistics(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting test statistics summary");

            var result = await _testResultsService.GetTestStatisticsAsync(cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test statistics");
            return StatusCode(500, ApiResponse<TestStatistics>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Bulk delete saved test results
    /// </summary>
    /// <param name="request">Bulk delete request with result IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully deleted results</returns>
    [HttpPost("saved/bulk-delete")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<BulkDeleteResultDto>>> BulkDeleteSavedTestResults(
        [FromBody] BulkDeleteTestResultsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk deleting {Count} saved test results", request.ResultIds.Count);

            int successCount = 0;
            var errors = new List<string>();

            foreach (var resultId in request.ResultIds)
            {
                try
                {
                    var deleteResult = await _testResultsService.DeleteSavedTestResultAsync(resultId, cancellationToken);
                    if (deleteResult.IsSuccess && deleteResult.Data == true)
                    {
                        successCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to delete result {resultId}: {deleteResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error deleting result {resultId}: {ex.Message}");
                }
            }

            var result = new BulkDeleteResultDto
            {
                TotalRequested = request.ResultIds.Count,
                SuccessfullyDeleted = successCount,
                FailedToDelete = request.ResultIds.Count - successCount,
                Errors = errors
            };

            return HandleResult(Result<BulkDeleteResultDto>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting saved test results");
            return StatusCode(500, ApiResponse<BulkDeleteResultDto>.FailureResult("Internal server error", 500));
        }
    }
}

/// <summary>
/// Request DTO for bulk delete operation
/// </summary>
public class BulkDeleteTestResultsRequestDto
{
    public List<string> ResultIds { get; set; } = new();
}

/// <summary>
/// Result DTO for bulk delete operation
/// </summary>
public class BulkDeleteResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessfullyDeleted { get; set; }
    public int FailedToDelete { get; set; }
    public List<string> Errors { get; set; } = new();
} 