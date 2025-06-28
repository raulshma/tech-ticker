namespace TechTicker.Application.Configuration;

/// <summary>
/// Configuration for proxy health monitoring
/// </summary>
public class ProxyHealthMonitorConfiguration
{
    public const string SectionName = "ProxyHealthMonitor";

    /// <summary>
    /// Interval between health check cycles in minutes
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum age of last health check before proxy is considered stale (in minutes)
    /// </summary>
    public int MaxAgeMinutes { get; set; } = 120;

    /// <summary>
    /// URL to use for testing proxy connectivity
    /// </summary>
    public string TestUrl { get; set; } = "https://httpbin.org/ip";

    /// <summary>
    /// Timeout for proxy tests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether health monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
