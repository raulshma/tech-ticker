namespace TechTicker.Application.Messages;

/// <summary>
/// Command to analyze a URL for product discovery
/// </summary>
public class AnalyzeUrlCommand
{
    public Guid CommandId { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string DiscoveryMethod { get; set; } = "URL_ANALYSIS";
    public bool AutoApprove { get; set; } = false;
    public decimal? AutoApprovalThreshold { get; set; }
    public Dictionary<string, string>? AdditionalContext { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Command to analyze multiple URLs in batch
/// </summary>
public class BulkAnalyzeUrlsCommand
{
    public Guid CommandId { get; set; } = Guid.NewGuid();
    public List<string> Urls { get; set; } = new();
    public Guid? UserId { get; set; }
    public string DiscoveryMethod { get; set; } = "BULK_IMPORT";
    public bool AutoApprove { get; set; } = false;
    public decimal? AutoApprovalThreshold { get; set; }
    public int MaxConcurrency { get; set; } = 5;
    public Dictionary<string, string>? AdditionalContext { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Command to process approval workflow
/// </summary>
public class ProcessApprovalCommand
{
    public Guid CommandId { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid ReviewerId { get; set; }
    public string Action { get; set; } = null!; // Approve, Reject, RequestModification, ApproveWithModifications
    public string? Comments { get; set; }
    public Dictionary<string, object>? Modifications { get; set; }
    public bool CreateProduct { get; set; } = true;
    public Guid? CategoryOverride { get; set; }
    public string? ProductNameOverride { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}