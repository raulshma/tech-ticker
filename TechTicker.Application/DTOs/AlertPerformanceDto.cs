namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for alert system performance metrics
/// </summary>
public class AlertSystemPerformanceDto
{
    public DateTimeOffset MetricsGeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }

    // Alert Evaluation Metrics
    public AlertEvaluationMetricsDto EvaluationMetrics { get; set; } = new();

    // Notification Delivery Metrics
    public NotificationDeliveryMetricsDto NotificationMetrics { get; set; } = new();

    // System Health Metrics
    public AlertSystemHealthDto SystemHealth { get; set; } = new();

    // Performance Trends
    public List<AlertPerformanceTrendDto> PerformanceTrends { get; set; } = new();
}

/// <summary>
/// DTO for alert evaluation performance metrics
/// </summary>
public class AlertEvaluationMetricsDto
{
    public int TotalPricePointsProcessed { get; set; }
    public int TotalAlertsEvaluated { get; set; }
    public int TotalAlertsTriggered { get; set; }
    public double AlertTriggerRate { get; set; }
    public TimeSpan AverageEvaluationTime { get; set; }
    public TimeSpan MaxEvaluationTime { get; set; }
    public TimeSpan MinEvaluationTime { get; set; }
    public int EvaluationErrors { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, int> TriggersByConditionType { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageEvaluationTimeByCondition { get; set; } = new();
}

/// <summary>
/// DTO for notification delivery performance metrics
/// </summary>
public class NotificationDeliveryMetricsDto
{
    public int TotalNotificationsSent { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public int PendingDeliveries { get; set; }
    public double DeliverySuccessRate { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
    public TimeSpan MaxDeliveryTime { get; set; }
    public TimeSpan MinDeliveryTime { get; set; }
    public Dictionary<string, int> DeliveriesByChannel { get; set; } = new(); // Discord, Email, etc.
    public Dictionary<string, double> SuccessRateByChannel { get; set; } = new();
    public List<NotificationFailureReasonDto> TopFailureReasons { get; set; } = new();
}

/// <summary>
/// DTO for alert system health metrics
/// </summary>
public class AlertSystemHealthDto
{
    public bool IsHealthy { get; set; }
    public List<string> HealthIssues { get; set; } = new();
    public int ActiveAlertRules { get; set; }
    public int InactiveAlertRules { get; set; }
    public int AlertRulesWithErrors { get; set; }
    public TimeSpan SystemUptime { get; set; }
    public double MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int QueueBacklog { get; set; }
    public TimeSpan AverageProcessingDelay { get; set; }
}

/// <summary>
/// DTO for performance trends over time
/// </summary>
public class AlertPerformanceTrendDto
{
    public DateTimeOffset Timestamp { get; set; }
    public int AlertsTriggered { get; set; }
    public int NotificationsSent { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// DTO for notification failure reasons
/// </summary>
public class NotificationFailureReasonDto
{
    public string Reason { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string? SuggestedAction { get; set; }
}

/// <summary>
/// DTO for alert rule performance analysis
/// </summary>
public class AlertRulePerformanceAnalysisDto
{
    public Guid AlertRuleId { get; set; }
    public string RuleDescription { get; set; } = null!;
    public string ConditionType { get; set; } = null!;
    public bool IsActive { get; set; }
    public int TimesTriggered { get; set; }
    public int NotificationsSent { get; set; }
    public double NotificationSuccessRate { get; set; }
    public TimeSpan AverageEvaluationTime { get; set; }
    public TimeSpan AverageNotificationTime { get; set; }
    public DateTimeOffset? LastTriggered { get; set; }
    public DateTimeOffset? LastSuccessfulNotification { get; set; }
    public List<string> RecentErrors { get; set; } = new();
    public PerformanceRating PerformanceRating { get; set; }
    public List<string> OptimizationSuggestions { get; set; } = new();
}

/// <summary>
/// Performance rating enumeration
/// </summary>
public enum PerformanceRating
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// DTO for real-time alert monitoring
/// </summary>
public class RealTimeAlertMonitoringDto
{
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public int AlertsInLastMinute { get; set; }
    public int AlertsInLastHour { get; set; }
    public int NotificationsInLastMinute { get; set; }
    public int NotificationsInLastHour { get; set; }
    public int CurrentQueueSize { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public List<RecentAlertActivityDto> RecentActivity { get; set; } = new();
    public List<AlertSystemEventDto> RecentEvents { get; set; } = new();
}

/// <summary>
/// DTO for recent alert activity
/// </summary>
public class RecentAlertActivityDto
{
    public DateTimeOffset Timestamp { get; set; }
    public Guid AlertRuleId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ConditionType { get; set; } = null!;
    public decimal TriggeringPrice { get; set; }
    public string Status { get; set; } = null!; // TRIGGERED, NOTIFIED, FAILED
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// DTO for alert system events
/// </summary>
public class AlertSystemEventDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = null!; // ERROR, WARNING, INFO
    public string Message { get; set; } = null!;
    public string? Component { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
