using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Detailed log entry for each scraper execution run
/// </summary>
public class ScraperRunLog
{
    [Key]
    public Guid RunId { get; set; }

    [Required]
    public Guid MappingId { get; set; }

    [Required]
    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public TimeSpan? Duration { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = null!; // STARTED, SUCCESS, FAILED, TIMEOUT, CANCELLED

    // Input parameters
    [Required]
    [MaxLength(2048)]
    public string TargetUrl { get; set; } = null!;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// JSON string of additional HTTP headers used
    /// </summary>
    public string? AdditionalHeaders { get; set; }

    /// <summary>
    /// JSON string of CSS selectors used for extraction
    /// </summary>
    public string? Selectors { get; set; }

    // HTTP response details
    public int? HttpStatusCode { get; set; }

    public TimeSpan? ResponseTime { get; set; }

    public long? ResponseSizeBytes { get; set; }

    // Proxy information
    /// <summary>
    /// Proxy host and port used for the request (e.g., "192.168.1.1:8080")
    /// </summary>
    [MaxLength(100)]
    public string? ProxyUsed { get; set; }

    /// <summary>
    /// ID of the proxy configuration used for the request
    /// </summary>
    public Guid? ProxyId { get; set; }

    // Extraction results
    [MaxLength(500)]
    public string? ExtractedProductName { get; set; }

    public decimal? ExtractedPrice { get; set; }

    [MaxLength(100)]
    public string? ExtractedStockStatus { get; set; }

    [MaxLength(200)]
    public string? ExtractedSellerName { get; set; }

    // Image extraction results
    /// <summary>
    /// Primary image URL extracted during scraping
    /// </summary>
    [MaxLength(2048)]
    public string? ExtractedPrimaryImageUrl { get; set; }

    /// <summary>
    /// Additional image URLs as JSON array
    /// </summary>
    public string? ExtractedAdditionalImageUrls { get; set; }

    /// <summary>
    /// Original scraped image URLs as JSON array (before processing)
    /// </summary>
    public string? ExtractedOriginalImageUrls { get; set; }

    /// <summary>
    /// Number of images successfully processed
    /// </summary>
    public int? ImageProcessingCount { get; set; }

    /// <summary>
    /// Number of images successfully uploaded/stored
    /// </summary>
    public int? ImageUploadCount { get; set; }

    /// <summary>
    /// Image scraping error message if any
    /// </summary>
    [MaxLength(500)]
    public string? ImageScrapingError { get; set; }

    // Error details
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? ErrorCode { get; set; }

    public string? ErrorStackTrace { get; set; }

    [MaxLength(50)]
    public string? ErrorCategory { get; set; } // NETWORK, PARSING, TIMEOUT, SELECTOR, AUTHENTICATION, etc.

    // Retry information
    public int AttemptNumber { get; set; } = 1;

    public Guid? ParentRunId { get; set; }

    // Performance metrics
    public TimeSpan? PageLoadTime { get; set; }

    public TimeSpan? ParsingTime { get; set; }

    // Raw data for debugging (limited size)
    [MaxLength(2000)]
    public string? RawHtmlSnippet { get; set; }

    [MaxLength(1000)]
    public string? DebugNotes { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(MappingId))]
    public ProductSellerMapping Mapping { get; set; } = null!;

    [ForeignKey(nameof(ParentRunId))]
    public ScraperRunLog? ParentRun { get; set; }

    public ICollection<ScraperRunLog> RetryAttempts { get; set; } = new List<ScraperRunLog>();

    // Helper properties
    public bool IsSuccessful => Status == "SUCCESS";

    public bool IsCompleted => CompletedAt.HasValue;

    public bool IsRetry => ParentRunId.HasValue;

    public string StatusDisplayName => Status switch
    {
        "STARTED" => "In Progress",
        "SUCCESS" => "Successful",
        "FAILED" => "Failed",
        "TIMEOUT" => "Timed Out",
        "CANCELLED" => "Cancelled",
        _ => Status
    };

    public string ErrorCategoryDisplayName => ErrorCategory switch
    {
        "NETWORK" => "Network Error",
        "PARSING" => "Data Parsing Error",
        "TIMEOUT" => "Request Timeout",
        "SELECTOR" => "CSS Selector Error",
        "AUTHENTICATION" => "Authentication Error",
        "RATE_LIMIT" => "Rate Limit Exceeded",
        "BLOCKED" => "Request Blocked",
        _ => ErrorCategory ?? "Unknown Error"
    };
}
