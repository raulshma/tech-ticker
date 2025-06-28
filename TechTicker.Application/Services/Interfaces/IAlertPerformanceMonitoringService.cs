using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for monitoring alert system performance and metrics
/// </summary>
public interface IAlertPerformanceMonitoringService
{
    /// <summary>
    /// Get comprehensive alert system performance metrics
    /// </summary>
    Task<Result<AlertSystemPerformanceDto>> GetSystemPerformanceAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Get performance analysis for a specific alert rule
    /// </summary>
    Task<Result<AlertRulePerformanceAnalysisDto>> GetAlertRulePerformanceAsync(Guid alertRuleId);

    /// <summary>
    /// Get real-time monitoring data
    /// </summary>
    Task<Result<RealTimeAlertMonitoringDto>> GetRealTimeMonitoringAsync();

    /// <summary>
    /// Get performance trends over time
    /// </summary>
    Task<Result<List<AlertPerformanceTrendDto>>> GetPerformanceTrendsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        TimeSpan interval);

    /// <summary>
    /// Record alert evaluation performance metrics
    /// </summary>
    Task RecordAlertEvaluationMetricsAsync(
        Guid alertRuleId,
        TimeSpan evaluationTime,
        bool wasTriggered,
        bool hadError = false,
        string? errorMessage = null);

    /// <summary>
    /// Record notification delivery performance metrics
    /// </summary>
    Task RecordNotificationDeliveryMetricsAsync(
        Guid alertRuleId,
        string notificationChannel,
        TimeSpan deliveryTime,
        bool wasSuccessful,
        string? errorMessage = null);

    /// <summary>
    /// Get alert system health status
    /// </summary>
    Task<Result<AlertSystemHealthDto>> GetSystemHealthAsync();

    /// <summary>
    /// Get top performing alert rules
    /// </summary>
    Task<Result<List<AlertRulePerformanceAnalysisDto>>> GetTopPerformingAlertRulesAsync(
        int count = 10,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Get poorly performing alert rules that need attention
    /// </summary>
    Task<Result<List<AlertRulePerformanceAnalysisDto>>> GetPoorlyPerformingAlertRulesAsync(
        int count = 10,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Generate performance report
    /// </summary>
    Task<Result<AlertPerformanceReportDto>> GeneratePerformanceReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate);

    /// <summary>
    /// Get notification delivery statistics by channel
    /// </summary>
    Task<Result<Dictionary<string, NotificationChannelStatsDto>>> GetNotificationChannelStatsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Record system event for monitoring
    /// </summary>
    Task RecordSystemEventAsync(string eventType, string message, string? component = null, Dictionary<string, object>? metadata = null);
}

/// <summary>
/// DTO for performance reports
/// </summary>
public class AlertPerformanceReportDto
{
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public AlertSystemPerformanceDto OverallPerformance { get; set; } = new();
    public List<AlertRulePerformanceAnalysisDto> TopPerformers { get; set; } = new();
    public List<AlertRulePerformanceAnalysisDto> PoorPerformers { get; set; } = new();
    public List<string> KeyInsights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> RawMetrics { get; set; } = new();
}

/// <summary>
/// DTO for notification channel statistics
/// </summary>
public class NotificationChannelStatsDto
{
    public string ChannelName { get; set; } = null!;
    public int TotalNotifications { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
    public TimeSpan MaxDeliveryTime { get; set; }
    public TimeSpan MinDeliveryTime { get; set; }
    public List<NotificationFailureReasonDto> TopFailureReasons { get; set; } = new();
    public Dictionary<string, int> DeliveriesByHour { get; set; } = new();
    public bool IsHealthy { get; set; }
    public List<string> HealthIssues { get; set; } = new();
}
