using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for AlertHistory responses
/// </summary>
public class AlertHistoryDto
{
    public Guid AlertHistoryId { get; set; }
    public Guid AlertRuleId { get; set; }
    public Guid UserId { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string ConditionType { get; set; } = null!;
    public string AlertType { get; set; } = null!;
    public decimal? ThresholdValue { get; set; }
    public decimal? PercentageValue { get; set; }
    public string? SpecificSellerName { get; set; }
    public string SellerName { get; set; } = null!;
    public decimal TriggeringPrice { get; set; }
    public string TriggeringStockStatus { get; set; } = null!;
    public string? ProductPageUrl { get; set; }
    public string RuleDescription { get; set; } = null!;
    public DateTimeOffset TriggeredAt { get; set; }
    public string NotificationStatus { get; set; } = null!;
    public string? NotificationError { get; set; }
    public DateTimeOffset? NotificationSentAt { get; set; }
    public bool WasAlertDeactivated { get; set; }
    public TimeSpan? NotificationDelay { get; set; }
    public bool IsNotificationSuccessful { get; set; }

    // Navigation properties
    public AlertRuleDto? AlertRule { get; set; }
    public UserDto? User { get; set; }
    public ProductDto? Product { get; set; }
}

/// <summary>
/// DTO for alert history statistics
/// </summary>
public class AlertHistoryStatsDto
{
    public int TotalAlerts { get; set; }
    public int SuccessfulNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public int PendingNotifications { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageNotificationDelay { get; set; }
    public Dictionary<string, int> AlertsByConditionType { get; set; } = new();
    public Dictionary<string, int> AlertsByProduct { get; set; } = new();
}

/// <summary>
/// DTO for alert history filters
/// </summary>
public class AlertHistoryFilterDto
{
    public Guid? UserId { get; set; }
    public Guid? AlertRuleId { get; set; }
    public Guid? ProductId { get; set; }
    public string? NotificationStatus { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
