using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for AlertRule responses
/// </summary>
public class AlertRuleDto
{
    public Guid AlertRuleId { get; set; }
    public Guid UserId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string ConditionType { get; set; } = null!;
    public string AlertType { get; set; } = null!;
    public decimal? ThresholdValue { get; set; }
    public decimal? PercentageValue { get; set; }
    public string? SpecificSellerName { get; set; }
    public int NotificationFrequencyMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastNotifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string RuleDescription { get; set; } = null!;
    
    // Navigation properties
    public UserDto? User { get; set; }
    public ProductDto? Product { get; set; }
}

/// <summary>
/// DTO for creating a new alert rule
/// </summary>
public class CreateAlertRuleDto : IValidatableObject
{
    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConditionType { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string AlertType { get; set; } = "RECURRING";

    [Range(0.01, double.MaxValue, ErrorMessage = "Threshold value must be greater than 0")]
    public decimal? ThresholdValue { get; set; }

    [Range(0.1, 100.0, ErrorMessage = "Percentage value must be between 0.1 and 100")]
    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Notification frequency must be at least 1 minute")]
    public int NotificationFrequencyMinutes { get; set; } = 1440;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate condition type
        var validConditionTypes = new[] { "PRICE_BELOW", "PERCENT_DROP_FROM_LAST", "BACK_IN_STOCK" };
        if (!validConditionTypes.Contains(ConditionType))
        {
            results.Add(new ValidationResult(
                $"ConditionType must be one of: {string.Join(", ", validConditionTypes)}",
                new[] { nameof(ConditionType) }));
        }

        // Validate alert type
        var validAlertTypes = new[] { "RECURRING", "ONE_SHOT" };
        if (!validAlertTypes.Contains(AlertType))
        {
            results.Add(new ValidationResult(
                $"AlertType must be one of: {string.Join(", ", validAlertTypes)}",
                new[] { nameof(AlertType) }));
        }

        // Validate condition-specific requirements
        switch (ConditionType)
        {
            case "PRICE_BELOW":
                if (!ThresholdValue.HasValue)
                {
                    results.Add(new ValidationResult(
                        "ThresholdValue is required for PRICE_BELOW condition",
                        new[] { nameof(ThresholdValue) }));
                }
                break;

            case "PERCENT_DROP_FROM_LAST":
                if (!PercentageValue.HasValue)
                {
                    results.Add(new ValidationResult(
                        "PercentageValue is required for PERCENT_DROP_FROM_LAST condition",
                        new[] { nameof(PercentageValue) }));
                }
                break;

            case "BACK_IN_STOCK":
                // No additional validation needed for BACK_IN_STOCK
                break;
        }

        return results;
    }
}

/// <summary>
/// DTO for updating an alert rule
/// </summary>
public class UpdateAlertRuleDto : IValidatableObject
{
    [MaxLength(50)]
    public string? ConditionType { get; set; }

    [MaxLength(20)]
    public string? AlertType { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Threshold value must be greater than 0")]
    public decimal? ThresholdValue { get; set; }

    [Range(0.1, 100.0, ErrorMessage = "Percentage value must be between 0.1 and 100")]
    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Notification frequency must be at least 1 minute")]
    public int? NotificationFrequencyMinutes { get; set; }

    public bool? IsActive { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate condition type if provided
        if (!string.IsNullOrEmpty(ConditionType))
        {
            var validConditionTypes = new[] { "PRICE_BELOW", "PERCENT_DROP_FROM_LAST", "BACK_IN_STOCK" };
            if (!validConditionTypes.Contains(ConditionType))
            {
                results.Add(new ValidationResult(
                    $"ConditionType must be one of: {string.Join(", ", validConditionTypes)}",
                    new[] { nameof(ConditionType) }));
            }
        }

        return results;
    }
}
