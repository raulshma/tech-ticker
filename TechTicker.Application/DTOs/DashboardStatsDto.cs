namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Total number of products in the system
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// Total number of categories in the system
    /// </summary>
    public int TotalCategories { get; set; }

    /// <summary>
    /// Number of active product-seller mappings
    /// </summary>
    public int ActiveMappings { get; set; }

    /// <summary>
    /// Number of active alert rules
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// Total number of users in the system (admin only)
    /// </summary>
    public int? TotalUsers { get; set; }

    /// <summary>
    /// Total number of proxy configurations
    /// </summary>
    public int TotalProxies { get; set; }

    /// <summary>
    /// Number of healthy proxies
    /// </summary>
    public int HealthyProxies { get; set; }

    /// <summary>
    /// Proxy health percentage
    /// </summary>
    public double ProxyHealthPercentage { get; set; }

    /// <summary>
    /// Number of scraper runs in the last 24 hours
    /// </summary>
    public int RecentScraperRuns { get; set; }

    /// <summary>
    /// Scraper success rate percentage
    /// </summary>
    public double ScraperSuccessRate { get; set; }

    /// <summary>
    /// Number of notifications sent in the last 24 hours
    /// </summary>
    public int RecentNotifications { get; set; }

    /// <summary>
    /// Notification delivery success rate percentage
    /// </summary>
    public double NotificationSuccessRate { get; set; }

    /// <summary>
    /// System health status
    /// </summary>
    public bool SystemHealthy { get; set; }

    /// <summary>
    /// Number of alerts triggered in the last 24 hours
    /// </summary>
    public int RecentAlerts { get; set; }
}
