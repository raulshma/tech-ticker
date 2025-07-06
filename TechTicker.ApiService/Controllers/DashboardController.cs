using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Dashboard operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics including product count, category count, active mappings, and active alerts. User count is included for admin users.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        // Check if user is admin to include user count
        var isAdmin = User.IsInRole("Admin");
        
        var result = await _dashboardService.GetDashboardStatsAsync(includeUserCount: isAdmin);
        return HandleResult(result);
    }

    /// <summary>
    /// Get comprehensive analytics dashboard data
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data (optional)</param>
    /// <param name="dateTo">End date for analytics data (optional)</param>
    /// <returns>Comprehensive analytics dashboard data including all system metrics</returns>
    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<AnalyticsDashboardDto>>> GetAnalyticsDashboard(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        // Check if user is admin to include user count
        var isAdmin = User.IsInRole("Admin");
        
        var result = await _dashboardService.GetAnalyticsDashboardAsync(
            dateFrom: dateFrom, 
            dateTo: dateTo, 
            includeUserCount: isAdmin);
        return HandleResult(result);
    }

    /// <summary>
    /// Get browser automation analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data (optional)</param>
    /// <param name="dateTo">End date for analytics data (optional)</param>
    /// <returns>Browser automation test analytics including success rates and trends</returns>
    [HttpGet("analytics/browser-automation")]
    public async Task<ActionResult<ApiResponse<BrowserAutomationAnalyticsDto>>> GetBrowserAutomationAnalytics(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _dashboardService.GetBrowserAutomationAnalyticsAsync(
            dateFrom: dateFrom, 
            dateTo: dateTo);
        return HandleResult(result);
    }

    /// <summary>
    /// Get alert system analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data (optional)</param>
    /// <param name="dateTo">End date for analytics data (optional)</param>
    /// <returns>Alert system performance analytics including top and poor performers</returns>
    [HttpGet("analytics/alert-system")]
    public async Task<ActionResult<ApiResponse<AlertSystemAnalyticsDto>>> GetAlertSystemAnalytics(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _dashboardService.GetAlertSystemAnalyticsAsync(
            dateFrom: dateFrom, 
            dateTo: dateTo);
        return HandleResult(result);
    }

    /// <summary>
    /// Get proxy management analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data (optional)</param>
    /// <param name="dateTo">End date for analytics data (optional)</param>
    /// <returns>Proxy management analytics including health status and usage statistics</returns>
    [HttpGet("analytics/proxy-management")]
    public async Task<ActionResult<ApiResponse<ProxyManagementAnalyticsDto>>> GetProxyManagementAnalytics(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _dashboardService.GetProxyManagementAnalyticsAsync(
            dateFrom: dateFrom, 
            dateTo: dateTo);
        return HandleResult(result);
    }

    /// <summary>
    /// Get scraping worker analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data (optional)</param>
    /// <param name="dateTo">End date for analytics data (optional)</param>
    /// <returns>Scraping worker analytics including success rates and seller performance</returns>
    [HttpGet("analytics/scraping-worker")]
    public async Task<ActionResult<ApiResponse<ScrapingWorkerAnalyticsDto>>> GetScrapingWorkerAnalytics(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _dashboardService.GetScrapingWorkerAnalyticsAsync(
            dateFrom: dateFrom, 
            dateTo: dateTo);
        return HandleResult(result);
    }

    /// <summary>
    /// Get real-time system status
    /// </summary>
    /// <returns>Real-time system status including current health and recent activity</returns>
    [HttpGet("analytics/real-time-status")]
    public async Task<ActionResult<ApiResponse<RealTimeSystemStatusDto>>> GetRealTimeSystemStatus()
    {
        var result = await _dashboardService.GetRealTimeSystemStatusAsync();
        return HandleResult(result);
    }
}
