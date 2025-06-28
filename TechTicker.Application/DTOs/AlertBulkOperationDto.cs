using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for bulk alert rule operations
/// </summary>
public class AlertBulkOperationRequestDto
{
    [Required]
    public List<Guid> AlertRuleIds { get; set; } = new();

    [Required]
    [MaxLength(50)]
    public string Operation { get; set; } = null!; // ENABLE, DISABLE, DELETE, UPDATE_FREQUENCY

    /// <summary>
    /// For UPDATE_FREQUENCY operation
    /// </summary>
    public int? NewNotificationFrequencyMinutes { get; set; }

    /// <summary>
    /// For UPDATE_TYPE operation
    /// </summary>
    [MaxLength(20)]
    public string? NewAlertType { get; set; }

    /// <summary>
    /// Reason for the bulk operation (for audit trail)
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for bulk operation results
/// </summary>
public class AlertBulkOperationResultDto
{
    public string Operation { get; set; } = null!;
    public int TotalRequested { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public List<AlertBulkOperationItemResultDto> Results { get; set; } = new();
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for individual bulk operation item results
/// </summary>
public class AlertBulkOperationItemResultDto
{
    public Guid AlertRuleId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// DTO for bulk alert rule filters
/// </summary>
public class AlertBulkFilterDto
{
    public Guid? UserId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ConditionType { get; set; }
    public string? AlertType { get; set; }
    public bool? IsActive { get; set; }
    public DateTimeOffset? CreatedAfter { get; set; }
    public DateTimeOffset? CreatedBefore { get; set; }
    public DateTimeOffset? LastNotifiedAfter { get; set; }
    public DateTimeOffset? LastNotifiedBefore { get; set; }
    public int? MinNotificationFrequency { get; set; }
    public int? MaxNotificationFrequency { get; set; }
}

/// <summary>
/// DTO for bulk operation with filters
/// </summary>
public class AlertBulkOperationWithFiltersRequestDto
{
    [Required]
    public AlertBulkFilterDto Filters { get; set; } = new();

    [Required]
    [MaxLength(50)]
    public string Operation { get; set; } = null!;

    /// <summary>
    /// For UPDATE_FREQUENCY operation
    /// </summary>
    public int? NewNotificationFrequencyMinutes { get; set; }

    /// <summary>
    /// For UPDATE_TYPE operation
    /// </summary>
    [MaxLength(20)]
    public string? NewAlertType { get; set; }

    /// <summary>
    /// Reason for the bulk operation
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Maximum number of alert rules to process (safety limit)
    /// </summary>
    public int MaxAffectedRules { get; set; } = 100;

    /// <summary>
    /// Preview mode - don't actually perform operations, just show what would be affected
    /// </summary>
    public bool PreviewOnly { get; set; } = false;
}

/// <summary>
/// DTO for bulk operation preview
/// </summary>
public class AlertBulkOperationPreviewDto
{
    public AlertBulkFilterDto Filters { get; set; } = new();
    public string Operation { get; set; } = null!;
    public int TotalMatchingRules { get; set; }
    public int RulesWouldBeAffected { get; set; }
    public List<AlertRuleDto> SampleAffectedRules { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool IsSafeToExecute { get; set; }
    public string? SafetyMessage { get; set; }
}
