using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for scraper run logs operations
/// </summary>
[Route("api/scraper-logs")]
[ApiController]
[Authorize]
public class ScraperLogsController : BaseApiController
{
    private readonly IScraperRunLogService _scraperRunLogService;

    public ScraperLogsController(IScraperRunLogService scraperRunLogService)
    {
        _scraperRunLogService = scraperRunLogService;
    }

    /// <summary>
    /// Get paginated scraper run logs with filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="mappingId">Filter by mapping ID</param>
    /// <param name="status">Filter by status (STARTED, SUCCESS, FAILED, TIMEOUT, CANCELLED)</param>
    /// <param name="errorCategory">Filter by error category</param>
    /// <param name="dateFrom">Filter by start date (inclusive)</param>
    /// <param name="dateTo">Filter by end date (inclusive)</param>
    /// <param name="sellerName">Filter by seller name (partial match)</param>
    /// <returns>Paginated list of scraper run logs</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ScraperRunLogSummaryDto>>>> GetScraperLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? mappingId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? errorCategory = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        [FromQuery] string? sellerName = null)
    {
        // Validate page size
        if (pageSize > 100)
            pageSize = 100;

        var filter = new ScraperRunLogFilterDto
        {
            Page = page,
            PageSize = pageSize,
            MappingId = mappingId,
            Status = status,
            ErrorCategory = errorCategory,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SellerName = sellerName
        };

        var result = await _scraperRunLogService.GetPagedLogsAsync(filter);
        return HandleResult(result);
    }

    /// <summary>
    /// Get detailed information for a specific scraper run
    /// </summary>
    /// <param name="runId">The run ID</param>
    /// <returns>Detailed scraper run log information</returns>
    [HttpGet("{runId:guid}")]
    public async Task<ActionResult<ApiResponse<ScraperRunLogDto>>> GetScraperRunById(Guid runId)
    {
        var result = await _scraperRunLogService.GetRunByIdAsync(runId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get scraper run logs for a specific mapping
    /// </summary>
    /// <param name="mappingId">The mapping ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 50)</param>
    /// <returns>Paginated list of scraper run logs for the mapping</returns>
    [HttpGet("mapping/{mappingId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ScraperRunLogSummaryDto>>>> GetScraperLogsByMapping(
        Guid mappingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 50)
            pageSize = 50;

        var result = await _scraperRunLogService.GetLogsByMappingIdAsync(mappingId, page, pageSize);
        return HandleResult(result);
    }

    /// <summary>
    /// Get recent failed scraper runs for dashboard
    /// </summary>
    /// <param name="count">Number of recent failed runs to return (default: 10, max: 50)</param>
    /// <returns>List of recent failed scraper runs</returns>
    [HttpGet("recent-failures")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScraperRunLogSummaryDto>>>> GetRecentFailures(
        [FromQuery] int count = 10)
    {
        if (count > 50)
            count = 50;

        var result = await _scraperRunLogService.GetRecentFailedRunsAsync(count);
        return HandleResult(result);
    }

    /// <summary>
    /// Get recent scraper runs for a specific mapping
    /// </summary>
    /// <param name="mappingId">The mapping ID</param>
    /// <param name="count">Number of recent runs to return (default: 5, max: 20)</param>
    /// <returns>List of recent scraper runs for the mapping</returns>
    [HttpGet("mapping/{mappingId:guid}/recent")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScraperRunLogSummaryDto>>>> GetRecentRunsForMapping(
        Guid mappingId,
        [FromQuery] int count = 5)
    {
        if (count > 20)
            count = 20;

        var result = await _scraperRunLogService.GetRecentRunsForMappingAsync(mappingId, count);
        return HandleResult(result);
    }

    /// <summary>
    /// Get scraper run statistics
    /// </summary>
    /// <param name="dateFrom">Start date for statistics (optional)</param>
    /// <param name="dateTo">End date for statistics (optional)</param>
    /// <param name="mappingId">Filter by specific mapping (optional)</param>
    /// <returns>Scraper run statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<ScraperRunStatisticsDto>>> GetStatistics(
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        [FromQuery] Guid? mappingId = null)
    {
        var result = await _scraperRunLogService.GetStatisticsAsync(dateFrom, dateTo, mappingId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get retry chain for a specific scraper run
    /// </summary>
    /// <param name="runId">The run ID</param>
    /// <returns>List of all runs in the retry chain</returns>
    [HttpGet("{runId:guid}/retry-chain")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScraperRunLogDto>>>> GetRetryChain(Guid runId)
    {
        var result = await _scraperRunLogService.GetRetryChainAsync(runId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get performance metrics by seller
    /// </summary>
    /// <param name="dateFrom">Start date for metrics (optional)</param>
    /// <param name="dateTo">End date for metrics (optional)</param>
    /// <returns>Performance metrics grouped by seller</returns>
    [HttpGet("performance-metrics")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SellerPerformanceMetricDto>>>> GetPerformanceMetrics(
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null)
    {
        var result = await _scraperRunLogService.GetPerformanceMetricsBySellerAsync(dateFrom, dateTo);
        return HandleResult(result);
    }

    /// <summary>
    /// Get scraper runs that are currently in progress
    /// </summary>
    /// <returns>List of in-progress scraper runs</returns>
    [HttpGet("in-progress")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScraperRunLogSummaryDto>>>> GetInProgressRuns()
    {
        var result = await _scraperRunLogService.GetInProgressRunsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Clean up old scraper run logs
    /// </summary>
    /// <param name="daysToKeep">Number of days to keep logs (default: 90)</param>
    /// <returns>Number of logs deleted</returns>
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        if (daysToKeep < 7)
        {
            return BadRequestGeneric<int>("Cannot delete logs newer than 7 days");
        }

        var result = await _scraperRunLogService.CleanupOldLogsAsync(daysToKeep);
        return HandleResult(result);
    }
}
