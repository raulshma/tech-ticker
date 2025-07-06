namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for browser automation analytics
/// </summary>
public class BrowserAutomationAnalyticsDto
{
    /// <summary>
    /// Test success/failure rates over time
    /// </summary>
    public List<TestSuccessRateDataPoint> SuccessRateTrend { get; set; } = new();

    /// <summary>
    /// Average execution time per test
    /// </summary>
    public List<TestExecutionTimeDataPoint> ExecutionTimeTrend { get; set; } = new();

    /// <summary>
    /// Most common failure points
    /// </summary>
    public List<FailurePointDto> CommonFailurePoints { get; set; } = new();

    /// <summary>
    /// Browser engine reliability
    /// </summary>
    public Dictionary<string, double> BrowserReliability { get; set; } = new();

    /// <summary>
    /// Popular test URLs
    /// </summary>
    public List<PopularTestUrlDto> PopularTestUrls { get; set; } = new();

    /// <summary>
    /// Flaky tests (URLs with both success and failure)
    /// </summary>
    public List<FlakyTestDto> FlakyTests { get; set; } = new();

    /// <summary>
    /// Overall statistics
    /// </summary>
    public TestExecutionStatisticsDto OverallStatistics { get; set; } = new();
}

/// <summary>
/// DTO for alert system analytics
/// </summary>
public class AlertSystemAnalyticsDto
{
    /// <summary>
    /// Alert trigger frequency over time
    /// </summary>
    public List<AlertTriggerDataPoint> TriggerFrequency { get; set; } = new();

    /// <summary>
    /// Notification delivery success rate
    /// </summary>
    public List<NotificationSuccessDataPoint> NotificationSuccessRate { get; set; } = new();

    /// <summary>
    /// Average response time for alerts
    /// </summary>
    public List<AlertResponseTimeDataPoint> ResponseTimeTrend { get; set; } = new();

    /// <summary>
    /// Top performing alert rules
    /// </summary>
    public List<AlertRulePerformanceAnalysisDto> TopPerformers { get; set; } = new();

    /// <summary>
    /// Poor performing alert rules
    /// </summary>
    public List<AlertRulePerformanceAnalysisDto> PoorPerformers { get; set; } = new();

    /// <summary>
    /// Alert system health metrics
    /// </summary>
    public AlertSystemHealthDto SystemHealth { get; set; } = new();
}

/// <summary>
/// DTO for proxy management analytics
/// </summary>
public class ProxyManagementAnalyticsDto
{
    /// <summary>
    /// Proxy health status over time
    /// </summary>
    public List<ProxyHealthDataPoint> HealthStatusTrend { get; set; } = new();

    /// <summary>
    /// Proxy failure rates
    /// </summary>
    public List<ProxyFailureDataPoint> FailureRateTrend { get; set; } = new();

    /// <summary>
    /// Performance impact of proxies on scraping
    /// </summary>
    public List<ProxyPerformanceDataPoint> PerformanceImpact { get; set; } = new();

    /// <summary>
    /// Proxy usage statistics
    /// </summary>
    public ProxyUsageStatisticsDto UsageStatistics { get; set; } = new();
}

/// <summary>
/// DTO for scraping worker analytics
/// </summary>
public class ScrapingWorkerAnalyticsDto
{
    /// <summary>
    /// Scraper success/failure rates over time
    /// </summary>
    public List<ScraperSuccessDataPoint> SuccessRateTrend { get; set; } = new();

    /// <summary>
    /// Average scraping time per product
    /// </summary>
    public List<ScraperTimeDataPoint> ScrapingTimeTrend { get; set; } = new();

    /// <summary>
    /// Most frequently scraped products
    /// </summary>
    public List<FrequentlyScrapedProductDto> FrequentlyScrapedProducts { get; set; } = new();

    /// <summary>
    /// Scraper performance by seller
    /// </summary>
    public List<SellerPerformanceDto> SellerPerformance { get; set; } = new();

    /// <summary>
    /// Overall scraper statistics
    /// </summary>
    public ScraperStatisticsDto OverallStatistics { get; set; } = new();
}

/// <summary>
/// DTO for comprehensive analytics dashboard
/// </summary>
public class AnalyticsDashboardDto
{
    /// <summary>
    /// Browser automation analytics
    /// </summary>
    public BrowserAutomationAnalyticsDto BrowserAutomation { get; set; } = new();

    /// <summary>
    /// Alert system analytics
    /// </summary>
    public AlertSystemAnalyticsDto AlertSystem { get; set; } = new();

    /// <summary>
    /// Proxy management analytics
    /// </summary>
    public ProxyManagementAnalyticsDto ProxyManagement { get; set; } = new();

    /// <summary>
    /// Scraping worker analytics
    /// </summary>
    public ScrapingWorkerAnalyticsDto ScrapingWorker { get; set; } = new();

    /// <summary>
    /// System-wide metrics
    /// </summary>
    public SystemWideMetricsDto SystemWideMetrics { get; set; } = new();

    /// <summary>
    /// Real-time system status
    /// </summary>
    public RealTimeSystemStatusDto RealTimeStatus { get; set; } = new();
}

// Data point DTOs for time series data
public class TestSuccessRateDataPoint
{
    public DateTime Date { get; set; }
    public double SuccessRate { get; set; }
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
}

public class TestExecutionTimeDataPoint
{
    public DateTime Date { get; set; }
    public double AverageExecutionTime { get; set; }
    public double MinExecutionTime { get; set; }
    public double MaxExecutionTime { get; set; }
    public int TestCount { get; set; }
}

public class AlertTriggerDataPoint
{
    public DateTime Date { get; set; }
    public int TriggerCount { get; set; }
    public int UniqueAlerts { get; set; }
    public double AverageResponseTime { get; set; }
}

public class NotificationSuccessDataPoint
{
    public DateTime Date { get; set; }
    public double SuccessRate { get; set; }
    public int TotalNotifications { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
}

public class AlertResponseTimeDataPoint
{
    public DateTime Date { get; set; }
    public double AverageResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public int AlertCount { get; set; }
}

public class ProxyHealthDataPoint
{
    public DateTime Date { get; set; }
    public int TotalProxies { get; set; }
    public int HealthyProxies { get; set; }
    public double HealthPercentage { get; set; }
}

public class ProxyFailureDataPoint
{
    public DateTime Date { get; set; }
    public int FailureCount { get; set; }
    public double FailureRate { get; set; }
    public int TotalRequests { get; set; }
}

public class ProxyPerformanceDataPoint
{
    public DateTime Date { get; set; }
    public double AverageResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public int RequestCount { get; set; }
}

public class ScraperSuccessDataPoint
{
    public DateTime Date { get; set; }
    public double SuccessRate { get; set; }
    public int TotalScrapes { get; set; }
    public int SuccessfulScrapes { get; set; }
    public int FailedScrapes { get; set; }
}

public class ScraperTimeDataPoint
{
    public DateTime Date { get; set; }
    public double AverageScrapingTime { get; set; }
    public double MinScrapingTime { get; set; }
    public double MaxScrapingTime { get; set; }
    public int ProductCount { get; set; }
}

// Supporting DTOs
public class FailurePointDto
{
    public string TestUrl { get; set; } = null!;
    public string ProfileHash { get; set; } = null!;
    public int FailureCount { get; set; }
    public double FailureRate { get; set; }
    public string? CommonError { get; set; }
}



public class FlakyTestDto
{
    public string TestUrl { get; set; } = null!;
    public string ProfileHash { get; set; } = null!;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double FlakinessScore { get; set; }
}

public class TestExecutionStatisticsDto
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime? FirstExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public int UniqueUrls { get; set; }
    public int UniqueProfiles { get; set; }
}



public class ProxyUsageStatisticsDto
{
    public int TotalProxies { get; set; }
    public int ActiveProxies { get; set; }
    public int HealthyProxies { get; set; }
    public double HealthPercentage { get; set; }
    public double AverageResponseTime { get; set; }
    public double OverallSuccessRate { get; set; }
    public int TotalRequests { get; set; }
    public int FailedRequests { get; set; }
}

public class FrequentlyScrapedProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int ScrapeCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageScrapingTime { get; set; }
    public DateTime LastScraped { get; set; }
}

public class SellerPerformanceDto
{
    public string SellerName { get; set; } = null!;
    public int TotalScrapes { get; set; }
    public int SuccessfulScrapes { get; set; }
    public double SuccessRate { get; set; }
    public double AverageScrapingTime { get; set; }
    public int ErrorCount { get; set; }
    public List<string> CommonErrors { get; set; } = new();
}

public class ScraperStatisticsDto
{
    public int TotalScrapes { get; set; }
    public int SuccessfulScrapes { get; set; }
    public double SuccessRate { get; set; }
    public double AverageScrapingTime { get; set; }
    public int UniqueProducts { get; set; }
    public int UniqueSellers { get; set; }
    public DateTime? FirstScrape { get; set; }
    public DateTime? LastScrape { get; set; }
}

public class SystemWideMetricsDto
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int ActiveMappings { get; set; }
    public int ActiveAlerts { get; set; }
    public int TotalUsers { get; set; }
    public int TotalProxies { get; set; }
    public double SystemUptime { get; set; }
    public double OverallHealthScore { get; set; }
}

public class RealTimeSystemStatusDto
{
    public bool SystemHealthy { get; set; }
    public int RecentAlerts { get; set; }
    public int RecentNotifications { get; set; }
    public double NotificationSuccessRate { get; set; }
    public int RecentScraperRuns { get; set; }
    public double ScraperSuccessRate { get; set; }
    public int HealthyProxies { get; set; }
    public double ProxyHealthPercentage { get; set; }
    public List<string> ActiveIssues { get; set; } = new();
    public DateTime LastUpdated { get; set; }
} 