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

/// <summary>
/// DTO for creating a new scraper run log
/// </summary>
public class CreateScraperRunLogDto
{
    public Guid MappingId { get; set; }
    public string TargetUrl { get; set; } = null!;
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
    public ScrapingSelectorsDto? Selectors { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public Guid? ParentRunId { get; set; }
    public string? DebugNotes { get; set; }
}

/// <summary>
/// DTO for updating a scraper run log
/// </summary>
public class UpdateScraperRunLogDto
{
    public int? HttpStatusCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public long? ResponseSizeBytes { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }
    public string? RawHtmlSnippet { get; set; }
    public string? DebugNotes { get; set; }
}

/// <summary>
/// DTO for completing a scraper run
/// </summary>
public class CompleteScraperRunDto
{
    public string? ExtractedProductName { get; set; }
    public decimal? ExtractedPrice { get; set; }
    public string? ExtractedStockStatus { get; set; }
    public string? ExtractedSellerName { get; set; }
    public string? ExtractedPrimaryImageUrl { get; set; }
    public List<string>? ExtractedAdditionalImageUrls { get; set; }
    public List<string>? ExtractedOriginalImageUrls { get; set; }
    public int? ImageProcessingCount { get; set; }
    public int? ImageUploadCount { get; set; }
    public string? ImageScrapingError { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }
    public string? RawHtmlSnippet { get; set; }
    public string? DebugNotes { get; set; }
}

/// <summary>
/// DTO for marking a scraper run as failed
/// </summary>
public class FailScraperRunDto
{
    public string ErrorMessage { get; set; } = null!;
    public string? ErrorCode { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? ErrorCategory { get; set; }
    public int? HttpStatusCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }
    public string? RawHtmlSnippet { get; set; }
    public string? DebugNotes { get; set; }
}
