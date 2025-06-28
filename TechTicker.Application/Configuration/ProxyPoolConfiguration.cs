namespace TechTicker.Application.Configuration;

/// <summary>
/// Configuration for proxy pool service
/// </summary>
public class ProxyPoolConfiguration
{
    public const string SectionName = "ProxyPool";

    /// <summary>
    /// Whether proxy pool is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Proxy selection strategy: ROUND_ROBIN, LEAST_USED, BEST_SUCCESS_RATE, RANDOM
    /// </summary>
    public string SelectionStrategy { get; set; } = "BEST_SUCCESS_RATE";

    /// <summary>
    /// Maximum consecutive failures before proxy is excluded from pool
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Cache duration for proxy pool in minutes
    /// </summary>
    public int CacheMinutes { get; set; } = 5;

    /// <summary>
    /// Cache duration for proxy pool statistics in minutes
    /// </summary>
    public int StatsCacheMinutes { get; set; } = 2;

    /// <summary>
    /// Timeout for proxy requests in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retries when all proxies fail
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}
