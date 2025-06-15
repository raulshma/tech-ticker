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
public class CreateAlertRuleDto
{
    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConditionType { get; set; } = null!;

    public decimal? ThresholdValue { get; set; }

    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    public int NotificationFrequencyMinutes { get; set; } = 1440;
}

/// <summary>
/// DTO for updating an alert rule
/// </summary>
public class UpdateAlertRuleDto
{
    [MaxLength(50)]
    public string? ConditionType { get; set; }

    public decimal? ThresholdValue { get; set; }

    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    public int? NotificationFrequencyMinutes { get; set; }

    public bool? IsActive { get; set; }
}
