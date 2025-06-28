using TechTicker.Domain.Entities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing proxy pool for scraping operations
/// </summary>
public interface IProxyPoolService
{
    /// <summary>
    /// Get the next available proxy for scraping
    /// </summary>
    Task<ProxyConfiguration?> GetNextProxyAsync(string? proxyType = null);

    /// <summary>
    /// Get a specific proxy by ID
    /// </summary>
    Task<ProxyConfiguration?> GetProxyByIdAsync(Guid proxyId);

    /// <summary>
    /// Record successful proxy usage
    /// </summary>
    Task RecordProxySuccessAsync(Guid proxyId, int responseTimeMs);

    /// <summary>
    /// Record failed proxy usage
    /// </summary>
    Task RecordProxyFailureAsync(Guid proxyId, string? errorMessage = null, string? errorCode = null);

    /// <summary>
    /// Check if proxy pool is enabled
    /// </summary>
    bool IsProxyPoolEnabled { get; }

    /// <summary>
    /// Get proxy pool statistics
    /// </summary>
    Task<ProxyPoolStatsDto> GetPoolStatsAsync();

    /// <summary>
    /// Refresh the proxy pool cache
    /// </summary>
    Task RefreshPoolAsync();
}

/// <summary>
/// DTO for proxy pool statistics
/// </summary>
public class ProxyPoolStatsDto
{
    public int TotalProxies { get; set; }
    public int AvailableProxies { get; set; }
    public int HealthyProxies { get; set; }
    public decimal AverageSuccessRate { get; set; }
    public string? LastRefreshed { get; set; }
    public Dictionary<string, int> ProxiesByType { get; set; } = new();
}
