using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for ProxyConfiguration operations
/// </summary>
public interface IProxyConfigurationRepository : IRepository<ProxyConfiguration>
{
    /// <summary>
    /// Get all active proxy configurations
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetActiveProxiesAsync();

    /// <summary>
    /// Get all healthy proxy configurations
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetHealthyProxiesAsync();

    /// <summary>
    /// Get proxy configurations by type
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetProxiesByTypeAsync(string proxyType);

    /// <summary>
    /// Get proxy configurations that need health checking
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetProxiesForHealthCheckAsync(TimeSpan maxAge);

    /// <summary>
    /// Get proxy by host and port
    /// </summary>
    Task<ProxyConfiguration?> GetByHostAndPortAsync(string host, int port);

    /// <summary>
    /// Get proxy configurations ordered by success rate
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetProxiesOrderedBySuccessRateAsync();

    /// <summary>
    /// Get proxy configurations with low success rate
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetUnreliableProxiesAsync(decimal minSuccessRate = 60.0m);

    /// <summary>
    /// Update proxy usage statistics
    /// </summary>
    Task UpdateProxyStatsAsync(Guid proxyId, bool success, string? errorMessage = null, string? errorCode = null);

    /// <summary>
    /// Bulk update proxy health status
    /// </summary>
    Task BulkUpdateHealthStatusAsync(IEnumerable<(Guid ProxyId, bool IsHealthy, string? ErrorMessage, string? ErrorCode)> updates);

    /// <summary>
    /// Get proxy usage statistics
    /// </summary>
    Task<(int Total, int Active, int Healthy, decimal AverageSuccessRate)> GetProxyStatsAsync();

    /// <summary>
    /// Get proxy configurations by IDs
    /// </summary>
    Task<IEnumerable<ProxyConfiguration>> GetByIdsAsync(IEnumerable<Guid> ids);
}
