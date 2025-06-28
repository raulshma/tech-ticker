using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for alert performance monitoring and metrics
/// </summary>
[Route("api/alert-performance")]
[ApiController]
[Authorize]
public class AlertPerformanceController : BaseApiController
{
    private readonly IAlertPerformanceMonitoringService _performanceService;

    public AlertPerformanceController(IAlertPerformanceMonitoringService performanceService)
    {
        _performanceService = performanceService;
    }

    /// <summary>
    /// Get comprehensive alert system performance metrics (Admin only)
    /// </summary>
    /// <param name="startDate">Start date for metrics</param>
    /// <param name="endDate">End date for metrics</param>
    /// <returns>System performance metrics</returns>
    [HttpGet("system")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertSystemPerformanceDto>>> GetSystemPerformance(
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var result = await _performanceService.GetSystemPerformanceAsync(startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Get performance analysis for a specific alert rule
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <returns>Alert rule performance analysis</returns>
    [HttpGet("alert-rule/{alertRuleId:guid}")]
    [RequirePermission(Permissions.AlertRulesRead)]
    public async Task<ActionResult<ApiResponse<AlertRulePerformanceAnalysisDto>>> GetAlertRulePerformance(
        Guid alertRuleId)
    {
        var result = await _performanceService.GetAlertRulePerformanceAsync(alertRuleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get real-time monitoring data (Admin only)
    /// </summary>
    /// <returns>Real-time monitoring data</returns>
    [HttpGet("real-time")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<RealTimeAlertMonitoringDto>>> GetRealTimeMonitoring()
    {
        var result = await _performanceService.GetRealTimeMonitoringAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get performance trends over time (Admin only)
    /// </summary>
    /// <param name="startDate">Start date for trends</param>
    /// <param name="endDate">End date for trends</param>
    /// <param name="intervalHours">Interval in hours for trend data points</param>
    /// <returns>Performance trends</returns>
    [HttpGet("trends")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<List<AlertPerformanceTrendDto>>>> GetPerformanceTrends(
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        [FromQuery] int intervalHours = 1)
    {
        var interval = TimeSpan.FromHours(intervalHours);
        var result = await _performanceService.GetPerformanceTrendsAsync(startDate, endDate, interval);
        return HandleResult(result);
    }

    /// <summary>
    /// Get alert system health status (Admin only)
    /// </summary>
    /// <returns>System health status</returns>
    [HttpGet("health")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertSystemHealthDto>>> GetSystemHealth()
    {
        var result = await _performanceService.GetSystemHealthAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get top performing alert rules (Admin only)
    /// </summary>
    /// <param name="count">Number of top performers to return</param>
    /// <param name="startDate">Start date for analysis</param>
    /// <param name="endDate">End date for analysis</param>
    /// <returns>Top performing alert rules</returns>
    [HttpGet("top-performers")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<List<AlertRulePerformanceAnalysisDto>>>> GetTopPerformingAlertRules(
        [FromQuery] int count = 10,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var result = await _performanceService.GetTopPerformingAlertRulesAsync(count, startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Get poorly performing alert rules that need attention (Admin only)
    /// </summary>
    /// <param name="count">Number of poor performers to return</param>
    /// <param name="startDate">Start date for analysis</param>
    /// <param name="endDate">End date for analysis</param>
    /// <returns>Poorly performing alert rules</returns>
    [HttpGet("poor-performers")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<List<AlertRulePerformanceAnalysisDto>>>> GetPoorlyPerformingAlertRules(
        [FromQuery] int count = 10,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var result = await _performanceService.GetPoorlyPerformingAlertRulesAsync(count, startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Generate comprehensive performance report (Admin only)
    /// </summary>
    /// <param name="startDate">Start date for report</param>
    /// <param name="endDate">End date for report</param>
    /// <returns>Performance report</returns>
    [HttpGet("report")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertPerformanceReportDto>>> GeneratePerformanceReport(
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate)
    {
        var result = await _performanceService.GeneratePerformanceReportAsync(startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Get notification delivery statistics by channel (Admin only)
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Notification channel statistics</returns>
    [HttpGet("notification-channels")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, NotificationChannelStatsDto>>>> GetNotificationChannelStats(
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var result = await _performanceService.GetNotificationChannelStatsAsync(startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Record a system event for monitoring (Admin only)
    /// </summary>
    /// <param name="request">System event data</param>
    /// <returns>Success response</returns>
    [HttpPost("system-event")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse>> RecordSystemEvent([FromBody] SystemEventRequestDto request)
    {
        await _performanceService.RecordSystemEventAsync(
            request.EventType, 
            request.Message, 
            request.Component, 
            request.Metadata);

        return Ok("System event recorded successfully");
    }
}

/// <summary>
/// DTO for system event recording requests
/// </summary>
public class SystemEventRequestDto
{
    public string EventType { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Component { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
