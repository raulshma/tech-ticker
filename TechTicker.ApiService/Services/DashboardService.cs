using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
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
    private readonly ITestExecutionHistoryRepository _testExecutionHistoryRepository;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
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
        ITestExecutionHistoryRepository testExecutionHistoryRepository,
        IAlertHistoryRepository alertHistoryRepository,
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
        _testExecutionHistoryRepository = testExecutionHistoryRepository;
        _alertHistoryRepository = alertHistoryRepository;
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

    public async Task<Result<AnalyticsDashboardDto>> GetAnalyticsDashboardAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null, 
        bool includeUserCount = false)
    {
        try
        {
            _logger.LogInformation("Getting comprehensive analytics dashboard data from {DateFrom} to {DateTo}", dateFrom, dateTo);

            var analytics = new AnalyticsDashboardDto();

            // Get browser automation analytics
            var browserAnalyticsResult = await GetBrowserAutomationAnalyticsAsync(dateFrom, dateTo);
            if (browserAnalyticsResult.IsSuccess)
            {
                analytics.BrowserAutomation = browserAnalyticsResult.Data!;
            }

            // Get alert system analytics
            var alertAnalyticsResult = await GetAlertSystemAnalyticsAsync(dateFrom, dateTo);
            if (alertAnalyticsResult.IsSuccess)
            {
                analytics.AlertSystem = alertAnalyticsResult.Data!;
            }

            // Get proxy management analytics
            var proxyAnalyticsResult = await GetProxyManagementAnalyticsAsync(dateFrom, dateTo);
            if (proxyAnalyticsResult.IsSuccess)
            {
                analytics.ProxyManagement = proxyAnalyticsResult.Data!;
            }

            // Get scraping worker analytics
            var scrapingAnalyticsResult = await GetScrapingWorkerAnalyticsAsync(dateFrom, dateTo);
            if (scrapingAnalyticsResult.IsSuccess)
            {
                analytics.ScrapingWorker = scrapingAnalyticsResult.Data!;
            }

            // Get system-wide metrics
            var systemWideResult = await GetSystemWideMetricsAsync(includeUserCount);
            if (systemWideResult.IsSuccess)
            {
                analytics.SystemWideMetrics = systemWideResult.Data!;
            }

            // Get real-time system status
            var realTimeResult = await GetRealTimeSystemStatusAsync();
            if (realTimeResult.IsSuccess)
            {
                analytics.RealTimeStatus = realTimeResult.Data!;
            }

            return Result<AnalyticsDashboardDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics dashboard data");
            return Result<AnalyticsDashboardDto>.Failure("An error occurred while retrieving analytics dashboard data.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<BrowserAutomationAnalyticsDto>> GetBrowserAutomationAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogInformation("Getting browser automation analytics from {DateFrom} to {DateTo}", dateFrom, dateTo);

            var analytics = new BrowserAutomationAnalyticsDto();

            // Get test execution statistics
            var (totalExecutions, successfulExecutions, successRate, averageExecutionTime, 
                 firstExecution, lastExecution, uniqueUrls, uniqueProfiles) = 
                await _testExecutionHistoryRepository.GetExecutionStatisticsAsync(dateFrom, dateTo);

            analytics.OverallStatistics = new TestExecutionStatisticsDto
            {
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                SuccessRate = successRate,
                AverageExecutionTime = averageExecutionTime,
                FirstExecution = firstExecution,
                LastExecution = lastExecution,
                UniqueUrls = uniqueUrls,
                UniqueProfiles = uniqueProfiles
            };

            // Get trends data
            var trendsData = await _testExecutionHistoryRepository.GetTrendsDataAsync(null, dateFrom, dateTo);
            
            // Group by day for success rate trend
            analytics.SuccessRateTrend = trendsData
                .GroupBy(h => h.ExecutedAt.Date)
                .Select(g => new TestSuccessRateDataPoint
                {
                    Date = g.Key,
                    TotalTests = g.Count(),
                    SuccessfulTests = g.Count(h => h.Success),
                    FailedTests = g.Count(h => !h.Success),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(h => h.Success) / g.Count() * 100 : 0
                })
                .OrderBy(t => t.Date)
                .ToList();

            // Group by day for execution time trend
            analytics.ExecutionTimeTrend = trendsData
                .GroupBy(h => h.ExecutedAt.Date)
                .Select(g => new TestExecutionTimeDataPoint
                {
                    Date = g.Key,
                    TestCount = g.Count(),
                    AverageExecutionTime = g.Count() > 0 ? g.Average(h => h.Duration) : 0,
                    MinExecutionTime = g.Count() > 0 ? g.Min(h => h.Duration) : 0,
                    MaxExecutionTime = g.Count() > 0 ? g.Max(h => h.Duration) : 0
                })
                .OrderBy(t => t.Date)
                .ToList();

            // Get browser reliability
            analytics.BrowserReliability = await _testExecutionHistoryRepository.GetReliabilityByBrowserAsync();

            // Get popular test URLs
            var popularUrls = await _testExecutionHistoryRepository.GetPopularTestUrlsAsync(10);
            analytics.PopularTestUrls = popularUrls.Select(u => new PopularTestUrlDto
            {
                Url = u.TestUrl,
                TestCount = u.TestCount,
                SuccessRate = u.SuccessRate,
                AverageExecutionTime = u.AverageExecutionTime,
                LastTested = u.LastTested
            }).ToList();

            // Get flaky tests
            var flakyTests = await _testExecutionHistoryRepository.GetFlakyTestsAsync();
            analytics.FlakyTests = flakyTests.Select(f => new FlakyTestDto
            {
                TestUrl = f.TestUrl,
                ProfileHash = f.ProfileHash,
                SuccessCount = f.SuccessCount,
                FailureCount = f.FailureCount,
                FlakinessScore = f.SuccessCount + f.FailureCount > 0 ? 
                    Math.Min(f.SuccessCount, f.FailureCount) / (double)(f.SuccessCount + f.FailureCount) * 100 : 0
            }).ToList();

            return Result<BrowserAutomationAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving browser automation analytics");
            return Result<BrowserAutomationAnalyticsDto>.Failure("An error occurred while retrieving browser automation analytics.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<AlertSystemAnalyticsDto>> GetAlertSystemAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogInformation("Getting alert system analytics from {DateFrom} to {DateTo}", dateFrom, dateTo);

            var analytics = new AlertSystemAnalyticsDto();

            // Get alert performance data
            var performanceResult = await _alertPerformanceService.GetSystemPerformanceAsync(
                dateFrom?.ToUniversalTime(), 
                dateTo?.ToUniversalTime());

            if (performanceResult.IsSuccess && performanceResult.Data != null)
            {
                var performance = performanceResult.Data;

                // Get top and poor performing alert rules
                var topPerformersResult = await _alertPerformanceService.GetTopPerformingAlertRulesAsync(5, dateFrom?.ToUniversalTime(), dateTo?.ToUniversalTime());
                if (topPerformersResult.IsSuccess && topPerformersResult.Data != null)
                {
                    analytics.TopPerformers = topPerformersResult.Data;
                }

                var poorPerformersResult = await _alertPerformanceService.GetPoorlyPerformingAlertRulesAsync(5, dateFrom?.ToUniversalTime(), dateTo?.ToUniversalTime());
                if (poorPerformersResult.IsSuccess && poorPerformersResult.Data != null)
                {
                    analytics.PoorPerformers = poorPerformersResult.Data;
                }

                // Get system health
                var healthResult = await _alertPerformanceService.GetSystemHealthAsync();
                if (healthResult.IsSuccess && healthResult.Data != null)
                {
                    analytics.SystemHealth = new AlertSystemHealthDto
                    {
                        IsHealthy = healthResult.Data.IsHealthy,
                        OverallSuccessRate = performance.NotificationMetrics.DeliverySuccessRate,
                        AverageResponseTime = performance.EvaluationMetrics.AverageEvaluationTime.TotalMilliseconds,
                        AlertsInLastHour = 0, // Will be populated from real-time monitoring
                        NotificationsInLastHour = 0, // Will be populated from real-time monitoring
                        HealthIssues = healthResult.Data.HealthIssues
                    };
                }
            }

            // Get real-time monitoring for current metrics
            var monitoringResult = await _alertPerformanceService.GetRealTimeMonitoringAsync();
            if (monitoringResult.IsSuccess && monitoringResult.Data != null)
            {
                analytics.SystemHealth.AlertsInLastHour = monitoringResult.Data.AlertsInLastHour;
                analytics.SystemHealth.NotificationsInLastHour = monitoringResult.Data.NotificationsInLastHour;
            }

            return Result<AlertSystemAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert system analytics");
            return Result<AlertSystemAnalyticsDto>.Failure("An error occurred while retrieving alert system analytics.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ProxyManagementAnalyticsDto>> GetProxyManagementAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogInformation("Getting proxy management analytics from {DateFrom} to {DateTo}", dateFrom, dateTo);

            var analytics = new ProxyManagementAnalyticsDto();

            // Get proxy statistics
            var proxiesResult = await _proxyService.GetAllProxiesAsync();
            if (proxiesResult.IsSuccess && proxiesResult.Data != null)
            {
                var proxies = proxiesResult.Data.ToList();
                
                analytics.UsageStatistics = new ProxyUsageStatisticsDto
                {
                    TotalProxies = proxies.Count,
                    ActiveProxies = proxies.Count(p => p.IsActive),
                    HealthyProxies = proxies.Count(p => p.IsHealthy),
                    HealthPercentage = proxies.Count > 0 ? (double)proxies.Count(p => p.IsHealthy) / proxies.Count * 100 : 0,
                    AverageResponseTime = proxies.Count > 0 ? proxies.Average(p => p.LastResponseTime ?? 0) : 0,
                    OverallSuccessRate = proxies.Count > 0 ? (double)proxies.Count(p => p.IsHealthy) / proxies.Count * 100 : 0,
                    TotalRequests = proxies.Sum(p => p.TotalRequests),
                    FailedRequests = proxies.Sum(p => p.FailedRequests)
                };
            }

            return Result<ProxyManagementAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving proxy management analytics");
            return Result<ProxyManagementAnalyticsDto>.Failure("An error occurred while retrieving proxy management analytics.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ScrapingWorkerAnalyticsDto>> GetScrapingWorkerAnalyticsAsync(
        DateTime? dateFrom = null, 
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogInformation("Getting scraping worker analytics from {DateFrom} to {DateTo}", dateFrom, dateTo);

            var analytics = new ScrapingWorkerAnalyticsDto();

            // Get scraper statistics
            var scraperFilter = new ScraperRunLogFilterDto
            {
                DateFrom = dateFrom?.ToUniversalTime(),
                DateTo = dateTo?.ToUniversalTime(),
                Page = 1,
                PageSize = 1000
            };

            var scraperResult = await _scraperRunLogService.GetPagedLogsAsync(scraperFilter);
            if (scraperResult.IsSuccess && scraperResult.Data != null)
            {
                var logs = scraperResult.Data.Items.ToList();
                
                analytics.OverallStatistics = new ScraperStatisticsDto
                {
                    TotalScrapes = logs.Count,
                    SuccessfulScrapes = logs.Count(l => l.Status == "COMPLETED"),
                    SuccessRate = logs.Count > 0 ? (double)logs.Count(l => l.Status == "COMPLETED") / logs.Count * 100 : 0,
                    AverageScrapingTime = logs.Count > 0 ? logs.Average(l => l.Duration?.TotalMilliseconds ?? 0) : 0,
                    UniqueProducts = logs.Select(l => l.MappingId).Distinct().Count(),
                    UniqueSellers = logs.Select(l => l.SellerName).Distinct().Count(),
                    FirstScrape = logs.Count > 0 ? logs.Min(l => l.StartedAt.DateTime) : null,
                    LastScrape = logs.Count > 0 ? logs.Max(l => l.CompletedAt?.DateTime ?? l.StartedAt.DateTime) : null
                };

                // Get seller performance
                var sellerPerformance = logs
                    .GroupBy(l => l.SellerName)
                    .Select(g => new SellerPerformanceDto
                    {
                        SellerName = g.Key ?? "Unknown",
                        TotalScrapes = g.Count(),
                        SuccessfulScrapes = g.Count(l => l.Status == "COMPLETED"),
                        SuccessRate = g.Count() > 0 ? (double)g.Count(l => l.Status == "COMPLETED") / g.Count() * 100 : 0,
                        AverageScrapingTime = g.Count() > 0 ? g.Average(l => l.Duration?.TotalMilliseconds ?? 0) : 0,
                        ErrorCount = g.Count(l => l.Status != "COMPLETED"),
                        CommonErrors = g.Where(l => l.Status != "COMPLETED")
                            .GroupBy(l => l.ErrorMessage)
                            .OrderByDescending(eg => eg.Count())
                            .Take(3)
                            .Select(eg => eg.Key ?? "Unknown error")
                            .ToList()
                    })
                    .OrderByDescending(s => s.TotalScrapes)
                    .Take(10)
                    .ToList();

                analytics.SellerPerformance = sellerPerformance;
            }

            return Result<ScrapingWorkerAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scraping worker analytics");
            return Result<ScrapingWorkerAnalyticsDto>.Failure("An error occurred while retrieving scraping worker analytics.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<RealTimeSystemStatusDto>> GetRealTimeSystemStatusAsync()
    {
        try
        {
            _logger.LogInformation("Getting real-time system status");

            var status = new RealTimeSystemStatusDto
            {
                LastUpdated = DateTime.UtcNow
            };

            // Get system health
            var healthResult = await _alertPerformanceService.GetSystemHealthAsync();
            if (healthResult.IsSuccess && healthResult.Data != null)
            {
                status.SystemHealthy = healthResult.Data.IsHealthy;
                status.ActiveIssues = healthResult.Data.HealthIssues;
            }

            // Get real-time monitoring
            var monitoringResult = await _alertPerformanceService.GetRealTimeMonitoringAsync();
            if (monitoringResult.IsSuccess && monitoringResult.Data != null)
            {
                status.RecentAlerts = monitoringResult.Data.AlertsInLastHour;
                status.RecentNotifications = monitoringResult.Data.NotificationsInLastHour;

                var recentActivity = monitoringResult.Data.RecentActivity;
                if (recentActivity.Count > 0)
                {
                    var successfulNotifications = recentActivity.Count(a => a.Status == "SENT");
                    status.NotificationSuccessRate = Math.Round((double)successfulNotifications / recentActivity.Count * 100, 1);
                }
                else
                {
                    status.NotificationSuccessRate = 100;
                }
            }

            // Get recent scraper runs
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
            var scraperFilter = new ScraperRunLogFilterDto
            {
                DateFrom = yesterday,
                DateTo = DateTimeOffset.UtcNow,
                Page = 1,
                PageSize = 1000
            };

            var scraperResult = await _scraperRunLogService.GetPagedLogsAsync(scraperFilter);
            if (scraperResult.IsSuccess && scraperResult.Data != null)
            {
                var logs = scraperResult.Data.Items.ToList();
                status.RecentScraperRuns = logs.Count;
                var successfulRuns = logs.Count(l => l.Status == "COMPLETED");
                status.ScraperSuccessRate = status.RecentScraperRuns > 0
                    ? Math.Round((double)successfulRuns / status.RecentScraperRuns * 100, 1)
                    : 0;
            }

            // Get proxy health
            var proxiesResult = await _proxyService.GetAllProxiesAsync();
            if (proxiesResult.IsSuccess && proxiesResult.Data != null)
            {
                var proxies = proxiesResult.Data.ToList();
                status.HealthyProxies = proxies.Count(p => p.IsHealthy);
                status.ProxyHealthPercentage = proxies.Count > 0
                    ? Math.Round((double)status.HealthyProxies / proxies.Count * 100, 1)
                    : 0;
            }

            return Result<RealTimeSystemStatusDto>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving real-time system status");
            return Result<RealTimeSystemStatusDto>.Failure("An error occurred while retrieving real-time system status.", "INTERNAL_ERROR");
        }
    }

    private async Task<Result<SystemWideMetricsDto>> GetSystemWideMetricsAsync(bool includeUserCount = false)
    {
        try
        {
            var metrics = new SystemWideMetricsDto();

            // Get basic counts
            var productsResult = await _productService.GetProductsAsync(pageNumber: 1, pageSize: 1);
            if (productsResult.IsSuccess && productsResult.Data != null)
            {
                metrics.TotalProducts = (int)productsResult.Data.Pagination.TotalCount;
            }

            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.IsSuccess && categoriesResult.Data != null)
            {
                metrics.TotalCategories = categoriesResult.Data.Count();
            }

            var mappingsResult = await _mappingService.GetActiveMappingsAsync();
            if (mappingsResult.IsSuccess && mappingsResult.Data != null)
            {
                metrics.ActiveMappings = mappingsResult.Data.Count();
            }

            var alertsResult = await _alertRuleService.GetAllAlertRulesAsync(pageNumber: 1, pageSize: 1);
            if (alertsResult.IsSuccess && alertsResult.Data != null)
            {
                metrics.ActiveAlerts = (int)alertsResult.Data.Pagination.TotalCount;
            }

            if (includeUserCount)
            {
                var usersResult = await _userService.GetAllUsersAsync(pageNumber: 1, pageSize: 1);
                if (usersResult.IsSuccess && usersResult.Data != null)
                {
                    metrics.TotalUsers = (int)usersResult.Data.Pagination.TotalCount;
                }
            }

            var proxiesResult = await _proxyService.GetAllProxiesAsync();
            if (proxiesResult.IsSuccess && proxiesResult.Data != null)
            {
                metrics.TotalProxies = proxiesResult.Data.Count();
            }

            // Calculate overall health score
            var healthResult = await _alertPerformanceService.GetSystemHealthAsync();
            if (healthResult.IsSuccess && healthResult.Data != null)
            {
                metrics.OverallHealthScore = healthResult.Data.IsHealthy ? 100 : 75; // Simplified scoring
            }

            return Result<SystemWideMetricsDto>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system-wide metrics");
            return Result<SystemWideMetricsDto>.Failure("An error occurred while retrieving system-wide metrics.", "INTERNAL_ERROR");
        }
    }
}
