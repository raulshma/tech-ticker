using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Tracks the approval workflow for product discovery candidates
/// </summary>
public class DiscoveryApprovalWorkflow
{
    [Key]
    public Guid WorkflowId { get; set; }

    [Required]
    public Guid CandidateId { get; set; }

    [Required]
    public Guid ReviewerId { get; set; }

    [Required]
    [MaxLength(50)]
    public WorkflowAction Action { get; set; }

    public string? Comments { get; set; }

    /// <summary>
    /// JSON object containing any modifications made during approval
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Modifications { get; set; }

    [Required]
    public DateTimeOffset ActionDate { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(CandidateId))]
    public ProductDiscoveryCandidate Candidate { get; set; } = null!;

    [ForeignKey(nameof(ReviewerId))]
    public ApplicationUser Reviewer { get; set; } = null!;

    // Helper Properties
    [NotMapped]
    public Dictionary<string, object>? ModificationsDict
    {
        get => string.IsNullOrEmpty(Modifications) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Modifications);
        set => Modifications = value == null ? null : JsonSerializer.Serialize(value);
    }
}

public enum WorkflowAction
{
    StartReview,
    Approve,
    Reject,
    RequestModification,
    ApproveWithModifications
}