using System.ComponentModel.DataAnnotations;
using TechTicker.Domain.Entities;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for ProductDiscoveryCandidate responses
/// </summary>
public class ProductDiscoveryCandidateDto
{
    public Guid CandidateId { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string ExtractedProductName { get; set; } = null!;
    public string? ExtractedManufacturer { get; set; }
    public string? ExtractedModelNumber { get; set; }
    public decimal? ExtractedPrice { get; set; }
    public string? ExtractedImageUrl { get; set; }
    public string? ExtractedDescription { get; set; }
    public Dictionary<string, object>? ExtractedSpecifications { get; set; }
    public Guid? SuggestedCategoryId { get; set; }
    public decimal CategoryConfidenceScore { get; set; }
    public Guid? SimilarProductId { get; set; }
    public decimal SimilarityScore { get; set; }
    public string DiscoveryMethod { get; set; } = null!;
    public Guid? DiscoveredByUserId { get; set; }
    public DateTimeOffset DiscoveredAt { get; set; }
    public DiscoveryStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public CategoryDto? SuggestedCategory { get; set; }
    public ProductDto? SimilarProduct { get; set; }
    public UserDto? DiscoveredByUser { get; set; }
}

/// <summary>
/// DTO for creating a product discovery candidate
/// </summary>
public class CreateProductDiscoveryCandidateDto
{
    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string ExtractedProductName { get; set; } = null!;
    
    [MaxLength(100)]
    public string? ExtractedManufacturer { get; set; }
    
    [MaxLength(100)]
    public string? ExtractedModelNumber { get; set; }
    
    public decimal? ExtractedPrice { get; set; }
    
    [MaxLength(2048)]
    public string? ExtractedImageUrl { get; set; }
    
    public string? ExtractedDescription { get; set; }
    public Dictionary<string, object>? ExtractedSpecifications { get; set; }
    public Guid? SuggestedCategoryId { get; set; }
    public decimal CategoryConfidenceScore { get; set; } = 0;
    public Guid? SimilarProductId { get; set; }
    public decimal SimilarityScore { get; set; } = 0;
    
    [Required]
    [MaxLength(50)]
    public string DiscoveryMethod { get; set; } = "URL_ANALYSIS";
    
    public Guid? DiscoveredByUserId { get; set; }
}

/// <summary>
/// Result of a discovery analysis operation
/// </summary>
public class DiscoveryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ProductDiscoveryCandidateDto> Candidates { get; set; } = new();
    public DiscoveryMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about the discovery operation
/// </summary>
public class DiscoveryMetadata
{
    public int ProcessedUrls { get; set; }
    public int SuccessfulExtractions { get; set; }
    public int FailedExtractions { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of product data extraction from a URL
/// </summary>
public class ProductExtractionResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? ExtractedProductName { get; set; }
    public string? ExtractedManufacturer { get; set; }
    public string? ExtractedModelNumber { get; set; }
    public decimal? ExtractedPrice { get; set; }
    public string? ExtractedImageUrl { get; set; }
    public string? ExtractedDescription { get; set; }
    public Dictionary<string, object>? ExtractedSpecifications { get; set; }
    public Dictionary<string, string>? RawMetadata { get; set; }
}

/// <summary>
/// Result of site compatibility analysis
/// </summary>
public class SiteCompatibilityResult
{
    public bool IsCompatible { get; set; }
    public string Domain { get; set; } = null!;
    public string? Reason { get; set; }
    public List<string> SupportedSelectors { get; set; } = new();
    public Dictionary<string, object>? SiteMetadata { get; set; }
}

/// <summary>
/// Result of approval/rejection workflow
/// </summary>
public class ApprovalResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid CandidateId { get; set; }
    public DiscoveryStatus NewStatus { get; set; }
    public Guid? CreatedProductId { get; set; }
    public string? WorkflowNotes { get; set; }
}

/// <summary>
/// Approval decision for a discovery candidate
/// </summary>
public class ApprovalDecision
{
    public WorkflowAction Action { get; set; }
    public string? Comments { get; set; }
    public Dictionary<string, object>? Modifications { get; set; }
    public bool CreateProduct { get; set; } = true;
    public Guid? CategoryOverride { get; set; }
    public string? ProductNameOverride { get; set; }
}

/// <summary>
/// Category prediction result
/// </summary>
public class CategoryPrediction
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public decimal ConfidenceScore { get; set; }
    public string PredictionMethod { get; set; } = null!; // ML, RULE_BASED, MANUAL
    public Dictionary<string, object>? PredictionMetadata { get; set; }
}

/// <summary>
/// Similar product matching result
/// </summary>
public class SimilarProductResult
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal SimilarityScore { get; set; }
    public string MatchingMethod { get; set; } = null!; // TEXT_SIMILARITY, SPECIFICATION_MATCH, IMAGE_MATCH
    public List<string> MatchingFields { get; set; } = new();
    public Dictionary<string, object>? MatchMetadata { get; set; }
}

/// <summary>
/// Request DTOs for API consumption
/// </summary>
public class AnalyzeUrlRequest
{
    [Required]
    [MaxLength(2048)]
    public string Url { get; set; } = null!;
    
    public Guid? UserId { get; set; }
    public bool AutoApprove { get; set; } = false;
    public decimal? AutoApprovalThreshold { get; set; }
}

public class BulkAnalyzeUrlsRequest
{
    [Required]
    public List<string> Urls { get; set; } = new();
    
    public Guid? UserId { get; set; }
    public bool AutoApprove { get; set; } = false;
    public decimal? AutoApprovalThreshold { get; set; }
    public int MaxConcurrency { get; set; } = 5;
}

public class CandidateFilterRequest
{
    public DiscoveryStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? DiscoveredByUserId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class ApprovalRequest
{
    [Required]
    public WorkflowAction Action { get; set; }
    
    public string? Comments { get; set; }
    public Dictionary<string, object>? Modifications { get; set; }
    public bool CreateProduct { get; set; } = true;
    public Guid? CategoryOverride { get; set; }
    public string? ProductNameOverride { get; set; }
}

public class RejectionRequest
{
    [Required]
    public string Reason { get; set; } = null!;
    
    public string? Comments { get; set; }
    public bool BlockDomain { get; set; } = false;
    public bool UseForTraining { get; set; } = true;
}

/// <summary>
/// Request DTO for report period queries
/// </summary>
public class ReportPeriodRequest
{
    /// <summary>
    /// Start date for the report period
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// End date for the report period
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Optional grouping for the report (daily, weekly, monthly)
    /// </summary>
    public string? Grouping { get; set; } = "daily";
}

/// <summary>
/// Enhanced bulk analyze request with better validation
/// </summary>
public class BulkAnalyzeRequest
{
    /// <summary>
    /// List of URLs to analyze
    /// </summary>
    [Required(ErrorMessage = "URLs are required")]
    [MinLength(1, ErrorMessage = "At least one URL must be provided")]
    [MaxLength(100, ErrorMessage = "Cannot analyze more than 100 URLs at once")]
    public List<string> Urls { get; set; } = new();

    /// <summary>
    /// Batch processing settings
    /// </summary>
    public int? BatchSize { get; set; } = 10;

    /// <summary>
    /// Optional discovery method override
    /// </summary>
    [MaxLength(50)]
    public string? DiscoveryMethod { get; set; }

    /// <summary>
    /// User ID for tracking
    /// </summary>
    public Guid? UserId { get; set; }
}