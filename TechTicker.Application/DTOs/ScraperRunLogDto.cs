namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for detailed scraper run log information
/// </summary>
public class ScraperRunLogDto
{
    public Guid RunId { get; set; }
    public Guid MappingId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; } = null!;
    public string StatusDisplayName { get; set; } = null!;

    // Input parameters
    public string TargetUrl { get; set; } = null!;
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
    public ScrapingSelectorsDto? Selectors { get; set; }

    // HTTP response details
    public int? HttpStatusCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public long? ResponseSizeBytes { get; set; }

    // Proxy information
    public string? ProxyUsed { get; set; }
    public Guid? ProxyId { get; set; }

    // Extraction results
    public string? ExtractedProductName { get; set; }
    public decimal? ExtractedPrice { get; set; }
    public string? ExtractedStockStatus { get; set; }
    public string? ExtractedSellerName { get; set; }

    // Image extraction results
    public string? ExtractedPrimaryImageUrl { get; set; }
    public List<string>? ExtractedAdditionalImageUrls { get; set; }
    public List<string>? ExtractedOriginalImageUrls { get; set; }
    public int? ImageProcessingCount { get; set; }
    public int? ImageUploadCount { get; set; }
    public string? ImageScrapingError { get; set; }

    // Specification parsing results
    public string? SpecificationData { get; set; }
    public string? SpecificationMetadata { get; set; }
    public int? SpecificationCount { get; set; }
    public string? SpecificationParsingStrategy { get; set; }
    public double? SpecificationQualityScore { get; set; }
    public long? SpecificationParsingTime { get; set; }
    public string? SpecificationError { get; set; }

    // Error details
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? ErrorCategory { get; set; }
    public string? ErrorCategoryDisplayName { get; set; }

    // Retry information
    public int AttemptNumber { get; set; }
    public Guid? ParentRunId { get; set; }
    public bool IsRetry { get; set; }

    // Performance metrics
    public TimeSpan? PageLoadTime { get; set; }
    public TimeSpan? ParsingTime { get; set; }

    // Debug information
    public string? RawHtmlSnippet { get; set; }
    public string? DebugNotes { get; set; }

    // Related data
    public ProductSellerMappingDto? Mapping { get; set; }
    public List<ScraperRunLogSummaryDto>? RetryAttempts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// DTO for scraper run log summary (for list views)
/// </summary>
public class ScraperRunLogSummaryDto
{
    public Guid RunId { get; set; }
    public Guid MappingId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; } = null!;
    public string StatusDisplayName { get; set; } = null!;
    public string TargetUrl { get; set; } = null!;
    public decimal? ExtractedPrice { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCategory { get; set; }
    public string? ErrorCategoryDisplayName { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsRetry { get; set; }
    public TimeSpan? ResponseTime { get; set; }

    // Mapping info
    public string SellerName { get; set; } = null!;
    public string ProductName { get; set; } = null!;
}

/// <summary>
/// DTO for scraper run statistics
/// </summary>
public class ScraperRunStatisticsDto
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
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
}

/// <summary>
/// DTO for seller performance metrics
/// </summary>
public class SellerPerformanceMetricDto
{
    public string SellerName { get; set; } = null!;
    public int TotalRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? AverageDuration { get; set; }
}

/// <summary>
/// DTO for filtering scraper run logs
/// </summary>
public class ScraperRunLogFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? MappingId { get; set; }
    public string? Status { get; set; }
    public string? ErrorCategory { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public string? SellerName { get; set; }
}

/// <summary>
/// DTO for paginated results
/// </summary>
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
