namespace TechTicker.Application.Messages;

/// <summary>
/// Event raised when a product discovery analysis is completed
/// </summary>
public class ProductDiscoveryEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid? CommandId { get; set; }
    public string EventType { get; set; } = null!; // AnalysisCompleted, CandidateCreated, ApprovalProcessed
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Guid> CandidateIds { get; set; } = new();
    public int ProcessedUrls { get; set; }
    public int SuccessfulExtractions { get; set; }
    public int FailedExtractions { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Guid? UserId { get; set; }
    public Dictionary<string, object>? EventData { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when a candidate is approved and product is created
/// </summary>
public class CandidateApprovedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid CreatedProductId { get; set; }
    public Guid ReviewerId { get; set; }
    public string Action { get; set; } = null!;
    public string? Comments { get; set; }
    public Dictionary<string, object>? Modifications { get; set; }
    public DateTimeOffset ApprovedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when a candidate is rejected
/// </summary>
public class CandidateRejectedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid ReviewerId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Comments { get; set; }
    public DateTimeOffset RejectedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when URL analysis fails
/// </summary>
public class UrlAnalysisFailedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public string? ErrorCode { get; set; }
    public Exception? Exception { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTimeOffset FailedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when category prediction is completed
/// </summary>
public class CategoryPredictionEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid? PredictedCategoryId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string PredictionMethod { get; set; } = null!;
    public Dictionary<string, object>? PredictionMetadata { get; set; }
    public DateTimeOffset PredictedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when similar products are found
/// </summary>
public class SimilarProductsFoundEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public List<SimilarProductMatch> SimilarProducts { get; set; } = new();
    public DateTimeOffset FoundAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Similar product match information
/// </summary>
public class SimilarProductMatch
{
    public Guid ProductId { get; set; }
    public decimal SimilarityScore { get; set; }
    public string MatchingMethod { get; set; } = null!;
    public List<string> MatchingFields { get; set; } = new();
}