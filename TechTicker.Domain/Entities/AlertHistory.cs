using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Tracks the history of triggered alerts for audit and analysis purposes
/// </summary>
public class AlertHistory
{
    [Key]
    public Guid AlertHistoryId { get; set; }

    [Required]
    public Guid AlertRuleId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConditionType { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string AlertType { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ThresholdValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    [Required]
    [MaxLength(100)]
    public string SellerName { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TriggeringPrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string TriggeringStockStatus { get; set; } = null!;

    [MaxLength(500)]
    public string? ProductPageUrl { get; set; }

    [Required]
    [MaxLength(200)]
    public string RuleDescription { get; set; } = null!;

    [Required]
    public DateTimeOffset TriggeredAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string NotificationStatus { get; set; } = "PENDING"; // PENDING, SENT, FAILED

    [MaxLength(500)]
    public string? NotificationError { get; set; }

    public DateTimeOffset? NotificationSentAt { get; set; }

    [Required]
    public bool WasAlertDeactivated { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(AlertRuleId))]
    public AlertRule AlertRule { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(CanonicalProductId))]
    public Product Product { get; set; } = null!;

    // Helper properties
    public TimeSpan? NotificationDelay => NotificationSentAt.HasValue 
        ? NotificationSentAt.Value - TriggeredAt 
        : null;

    public bool IsNotificationSuccessful => NotificationStatus == "SENT";
}
