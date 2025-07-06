using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Dashboard operations
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <param name="includeUserCount">Whether to include user count (admin only)</param>
    /// <returns>Dashboard statistics</returns>
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(bool includeUserCount = false);

    /// <summary>
    /// Get comprehensive analytics dashboard data
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data</param>
    /// <param name="dateTo">End date for analytics data</param>
    /// <param name="includeUserCount">Whether to include user count (admin only)</param>
    /// <returns>Comprehensive analytics dashboard data</returns>
    Task<Result<AnalyticsDashboardDto>> GetAnalyticsDashboardAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null, 
        bool includeUserCount = false);

    /// <summary>
    /// Get browser automation analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data</param>
    /// <param name="dateTo">End date for analytics data</param>
    /// <returns>Browser automation analytics</returns>
    Task<Result<BrowserAutomationAnalyticsDto>> GetBrowserAutomationAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null);

    /// <summary>
    /// Get alert system analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data</param>
    /// <param name="dateTo">End date for analytics data</param>
    /// <returns>Alert system analytics</returns>
    Task<Result<AlertSystemAnalyticsDto>> GetAlertSystemAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null);

    /// <summary>
    /// Get proxy management analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data</param>
    /// <param name="dateTo">End date for analytics data</param>
    /// <returns>Proxy management analytics</returns>
    Task<Result<ProxyManagementAnalyticsDto>> GetProxyManagementAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null);

    /// <summary>
    /// Get scraping worker analytics
    /// </summary>
    /// <param name="dateFrom">Start date for analytics data</param>
    /// <param name="dateTo">End date for analytics data</param>
    /// <returns>Scraping worker analytics</returns>
    Task<Result<ScrapingWorkerAnalyticsDto>> GetScrapingWorkerAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null);

    /// <summary>
    /// Get real-time system status
    /// </summary>
    /// <returns>Real-time system status</returns>
    Task<Result<RealTimeSystemStatusDto>> GetRealTimeSystemStatusAsync();
}
