using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// User-defined alert rules for price changes
/// </summary>
public class AlertRule
{
    [Key]
    public Guid AlertRuleId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConditionType { get; set; } = null!; // PRICE_BELOW, PERCENT_DROP_FROM_LAST, BACK_IN_STOCK

    [Required]
    [MaxLength(20)]
    public string AlertType { get; set; } = "RECURRING"; // RECURRING, ONE_SHOT

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ThresholdValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    public int NotificationFrequencyMinutes { get; set; } = 1440; // Default 24 hours

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastNotifiedAt { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [ForeignKey(nameof(CanonicalProductId))]
    public Product Product { get; set; } = null!;

    // Helper properties
    public string RuleDescription
    {
        get
        {
            return ConditionType switch
            {
                "PRICE_BELOW" => $"Price below ${ThresholdValue:F2}",
                "PERCENT_DROP_FROM_LAST" => $"{PercentageValue:F1}% drop from last price",
                "BACK_IN_STOCK" => "Back in stock",
                _ => "Unknown condition"
            };
        }
    }
}
