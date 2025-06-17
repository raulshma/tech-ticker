using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Represents a product discovered from a URL that requires admin review
/// </summary>
public class ProductDiscoveryCandidate
{
    [Key]
    public Guid CandidateId { get; set; }

    // Source Information
    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = null!;

    // Extracted Product Data
    [Required]
    [MaxLength(500)]
    public string ExtractedProductName { get; set; } = null!;

    [MaxLength(100)]
    public string? ExtractedManufacturer { get; set; }

    [MaxLength(100)]
    public string? ExtractedModelNumber { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ExtractedPrice { get; set; }

    [MaxLength(2048)]
    public string? ExtractedImageUrl { get; set; }

    public string? ExtractedDescription { get; set; }

    /// <summary>
    /// JSON object containing extracted specifications
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ExtractedSpecifications { get; set; }

    // AI/ML Insights
    public Guid? SuggestedCategoryId { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal CategoryConfidenceScore { get; set; } = 0;

    public Guid? SimilarProductId { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal SimilarityScore { get; set; } = 0;

    // Discovery Metadata
    [Required]
    [MaxLength(50)]
    public string DiscoveryMethod { get; set; } = null!; // URL_ANALYSIS, BULK_IMPORT, MANUAL

    public Guid? DiscoveredByUserId { get; set; }

    [Required]
    public DateTimeOffset DiscoveredAt { get; set; }

    // Workflow Status
    [Required]
    [MaxLength(50)]
    public DiscoveryStatus Status { get; set; } = DiscoveryStatus.Pending;

    public string? RejectionReason { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(SuggestedCategoryId))]
    public Category? SuggestedCategory { get; set; }

    [ForeignKey(nameof(SimilarProductId))]
    public Product? SimilarProduct { get; set; }

    [ForeignKey(nameof(DiscoveredByUserId))]
    public ApplicationUser? DiscoveredByUser { get; set; }

    public ICollection<DiscoveryApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<DiscoveryApprovalWorkflow>();

    // Helper Properties
    [NotMapped]
    public Dictionary<string, object>? ExtractedSpecificationsDict
    {
        get => string.IsNullOrEmpty(ExtractedSpecifications) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(ExtractedSpecifications);
        set => ExtractedSpecifications = value == null ? null : JsonSerializer.Serialize(value);
    }
}

public enum DiscoveryStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    RequiresMoreInfo
}