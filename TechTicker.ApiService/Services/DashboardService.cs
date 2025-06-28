using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for Dashboard operations
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IProductSellerMappingService _mappingService;
    private readonly IAlertRuleService _alertRuleService;
    private readonly IUserService _userService;
    private readonly IProxyService _proxyService;
    private readonly IScraperRunLogService _scraperRunLogService;
    private readonly IAlertPerformanceMonitoringService _alertPerformanceService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IProductService productService,
        ICategoryService categoryService,
        IProductSellerMappingService mappingService,
        IAlertRuleService alertRuleService,
        IUserService userService,
        IProxyService proxyService,
        IScraperRunLogService scraperRunLogService,
        IAlertPerformanceMonitoringService alertPerformanceService,
        ILogger<DashboardService> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _mappingService = mappingService;
        _alertRuleService = alertRuleService;
        _userService = userService;
        _proxyService = proxyService;
        _scraperRunLogService = scraperRunLogService;
        _alertPerformanceService = alertPerformanceService;
        _logger = logger;
    }

    public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(bool includeUserCount = false)
    {
        try
        {
            _logger.LogInformation("Getting dashboard statistics, includeUserCount: {IncludeUserCount}", includeUserCount);

            var stats = new DashboardStatsDto();

            // Get total products count
            var productsResult = await _productService.GetProductsAsync(pageNumber: 1, pageSize: 1);
            if (productsResult.IsSuccess && productsResult.Data != null)
            {
                stats.TotalProducts = (int)productsResult.Data.Pagination.TotalCount;
            }
            else
            {
                _logger.LogWarning("Failed to get products count: {Error}", productsResult.ErrorMessage);
                stats.TotalProducts = 0;
            }

            // Get total categories count
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.IsSuccess && categoriesResult.Data != null)
            {
                stats.TotalCategories = categoriesResult.Data.Count();
            }
            else
            {
                _logger.LogWarning("Failed to get categories count: {Error}", categoriesResult.ErrorMessage);
                stats.TotalCategories = 0;
            }

            // Get active mappings count
            var mappingsResult = await _mappingService.GetActiveMappingsAsync();
            if (mappingsResult.IsSuccess && mappingsResult.Data != null)
            {
                stats.ActiveMappings = mappingsResult.Data.Count();
            }
            else
            {
                _logger.LogWarning("Failed to get active mappings count: {Error}", mappingsResult.ErrorMessage);
                stats.ActiveMappings = 0;
            }

            // Get active alerts count
            var alertsResult = await _alertRuleService.GetAllAlertRulesAsync(pageNumber: 1, pageSize: 1);
            if (alertsResult.IsSuccess && alertsResult.Data != null)
            {
                stats.ActiveAlerts = (int)alertsResult.Data.Pagination.TotalCount;
            }
            else
            {
                _logger.LogWarning("Failed to get alerts count: {Error}", alertsResult.ErrorMessage);
                stats.ActiveAlerts = 0;
            }

            // Get total users count (admin only)
            if (includeUserCount)
            {
                var usersResult = await _userService.GetAllUsersAsync(pageNumber: 1, pageSize: 1);
                if (usersResult.IsSuccess && usersResult.Data != null)
                {
                    stats.TotalUsers = (int)usersResult.Data.Pagination.TotalCount;
                }
                else
                {
                    _logger.LogWarning("Failed to get users count: {Error}", usersResult.ErrorMessage);
                    stats.TotalUsers = 0;
                }
            }

            // Get proxy statistics
            try
            {
                var proxiesResult = await _proxyService.GetAllProxiesAsync();
                if (proxiesResult.IsSuccess && proxiesResult.Data != null)
                {
                    var proxies = proxiesResult.Data.ToList();
                    stats.TotalProxies = proxies.Count;
                    stats.HealthyProxies = proxies.Count(p => p.IsHealthy);
                    stats.ProxyHealthPercentage = stats.TotalProxies > 0
                        ? Math.Round((double)stats.HealthyProxies / stats.TotalProxies * 100, 1)
                        : 0;
                }
                else
                {
                    _logger.LogWarning("Failed to get proxy statistics: {Error}", proxiesResult.ErrorMessage);
                    stats.TotalProxies = 0;
                    stats.HealthyProxies = 0;
                    stats.ProxyHealthPercentage = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving proxy statistics");
                stats.TotalProxies = 0;
                stats.HealthyProxies = 0;
                stats.ProxyHealthPercentage = 0;
            }

            // Get scraper statistics (last 24 hours)
            try
            {
                var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
                var scraperFilter = new ScraperRunLogFilterDto
                {
                    DateFrom = yesterday,
                    DateTo = DateTimeOffset.UtcNow,
                    Page = 1,
                    PageSize = 1000 // Get enough to calculate stats
                };

                var scraperResult = await _scraperRunLogService.GetPagedLogsAsync(scraperFilter);
                if (scraperResult.IsSuccess && scraperResult.Data != null)
                {
                    var logs = scraperResult.Data.Items.ToList();
                    stats.RecentScraperRuns = logs.Count;
                    var successfulRuns = logs.Count(l => l.Status == "COMPLETED");
                    stats.ScraperSuccessRate = stats.RecentScraperRuns > 0
                        ? Math.Round((double)successfulRuns / stats.RecentScraperRuns * 100, 1)
                        : 0;
                }
                else
                {
                    _logger.LogWarning("Failed to get scraper statistics: {Error}", scraperResult.ErrorMessage);
                    stats.RecentScraperRuns = 0;
                    stats.ScraperSuccessRate = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving scraper statistics");
                stats.RecentScraperRuns = 0;
                stats.ScraperSuccessRate = 0;
            }

            // Get alert performance and system health
            try
            {
                var healthResult = await _alertPerformanceService.GetSystemHealthAsync();
                if (healthResult.IsSuccess && healthResult.Data != null)
                {
                    stats.SystemHealthy = healthResult.Data.IsHealthy;
                }
                else
                {
                    _logger.LogWarning("Failed to get system health: {Error}", healthResult.ErrorMessage);
                    stats.SystemHealthy = false;
                }

                var monitoringResult = await _alertPerformanceService.GetRealTimeMonitoringAsync();
                if (monitoringResult.IsSuccess && monitoringResult.Data != null)
                {
                    stats.RecentAlerts = monitoringResult.Data.AlertsInLastHour;
                    stats.RecentNotifications = monitoringResult.Data.NotificationsInLastHour;

                    // Calculate notification success rate from recent activity
                    var recentActivity = monitoringResult.Data.RecentActivity;
                    if (recentActivity.Count > 0)
                    {
                        var successfulNotifications = recentActivity.Count(a => a.Status == "SENT");
                        stats.NotificationSuccessRate = Math.Round((double)successfulNotifications / recentActivity.Count * 100, 1);
                    }
                    else
                    {
                        stats.NotificationSuccessRate = 100; // Default to 100% if no recent activity
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to get alert monitoring data: {Error}", monitoringResult.ErrorMessage);
                    stats.RecentAlerts = 0;
                    stats.RecentNotifications = 0;
                    stats.NotificationSuccessRate = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving alert performance statistics");
                stats.SystemHealthy = false;
                stats.RecentAlerts = 0;
                stats.RecentNotifications = 0;
                stats.NotificationSuccessRate = 0;
            }

            _logger.LogInformation("Dashboard statistics retrieved successfully: Products={Products}, Categories={Categories}, Mappings={Mappings}, Alerts={Alerts}, Users={Users}, Proxies={Proxies}, SystemHealthy={SystemHealthy}",
                stats.TotalProducts, stats.TotalCategories, stats.ActiveMappings, stats.ActiveAlerts, stats.TotalUsers, stats.TotalProxies, stats.SystemHealthy);

            return Result<DashboardStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return Result<DashboardStatsDto>.Failure("An error occurred while retrieving dashboard statistics.", "INTERNAL_ERROR");
        }
    }
}
