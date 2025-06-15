namespace TechTicker.Application.Messages;

/// <summary>
/// Event published when an alert rule is triggered
/// </summary>
public class AlertTriggeredEvent
{
    public Guid AlertRuleId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = null!;
    public string? UserFirstName { get; set; }
    public Guid CanonicalProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductCategoryName { get; set; }
    public string SellerName { get; set; } = null!;
    public decimal TriggeringPrice { get; set; }
    public string TriggeringStockStatus { get; set; } = null!;
    public string RuleDescription { get; set; } = null!;
    public string ProductPageUrl { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
}
