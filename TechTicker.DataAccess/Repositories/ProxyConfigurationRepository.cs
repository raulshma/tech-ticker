using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for ProxyConfiguration operations
/// </summary>
public class ProxyConfigurationRepository : Repository<ProxyConfiguration>, IProxyConfigurationRepository
{
    public ProxyConfigurationRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetActiveProxiesAsync()
    {
        return await _context.ProxyConfigurations
            .Where(p => p.IsActive)
            .OrderBy(p => p.Host)
            .ThenBy(p => p.Port)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetHealthyProxiesAsync()
    {
        return await _context.ProxyConfigurations
            .Where(p => p.IsActive && p.IsHealthy)
            .OrderByDescending(p => p.SuccessRate)
            .ThenBy(p => p.ConsecutiveFailures)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetProxiesByTypeAsync(string proxyType)
    {
        return await _context.ProxyConfigurations
            .Where(p => p.ProxyType == proxyType && p.IsActive)
            .OrderByDescending(p => p.SuccessRate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetProxiesForHealthCheckAsync(TimeSpan maxAge)
    {
        var cutoffTime = DateTimeOffset.UtcNow - maxAge;
        
        return await _context.ProxyConfigurations
            .Where(p => p.IsActive && 
                       (p.LastTestedAt == null || p.LastTestedAt < cutoffTime))
            .OrderBy(p => p.LastTestedAt ?? DateTimeOffset.MinValue)
            .ToListAsync();
    }

    public async Task<ProxyConfiguration?> GetByHostAndPortAsync(string host, int port)
    {
        return await _context.ProxyConfigurations
            .FirstOrDefaultAsync(p => p.Host == host && p.Port == port);
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetProxiesOrderedBySuccessRateAsync()
    {
        return await _context.ProxyConfigurations
            .Where(p => p.IsActive && p.IsHealthy)
            .OrderByDescending(p => p.SuccessRate)
            .ThenBy(p => p.ConsecutiveFailures)
            .ThenByDescending(p => p.LastUsedAt ?? DateTimeOffset.MinValue)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetUnreliableProxiesAsync(decimal minSuccessRate = 60.0m)
    {
        return await _context.ProxyConfigurations
            .Where(p => p.IsActive && 
                       (p.SuccessRate < minSuccessRate || p.ConsecutiveFailures >= 3))
            .OrderBy(p => p.SuccessRate)
            .ThenByDescending(p => p.ConsecutiveFailures)
            .ToListAsync();
    }

    public async Task UpdateProxyStatsAsync(Guid proxyId, bool success, string? errorMessage = null, string? errorCode = null)
    {
        var proxy = await GetByIdAsync(proxyId);
        if (proxy == null) return;

        if (success)
        {
            proxy.RecordSuccess();
        }
        else
        {
            proxy.RecordFailure(errorMessage, errorCode);
        }

        _context.ProxyConfigurations.Update(proxy);
    }

    public async Task BulkUpdateHealthStatusAsync(IEnumerable<(Guid ProxyId, bool IsHealthy, string? ErrorMessage, string? ErrorCode)> updates)
    {
        var proxyIds = updates.Select(u => u.ProxyId).ToList();
        var proxies = await _context.ProxyConfigurations
            .Where(p => proxyIds.Contains(p.ProxyConfigurationId))
            .ToListAsync();

        foreach (var update in updates)
        {
            var proxy = proxies.FirstOrDefault(p => p.ProxyConfigurationId == update.ProxyId);
            if (proxy != null)
            {
                proxy.UpdateHealthStatus(update.IsHealthy, update.ErrorMessage, update.ErrorCode);
            }
        }

        _context.ProxyConfigurations.UpdateRange(proxies);
    }

    public async Task<(int Total, int Active, int Healthy, decimal AverageSuccessRate)> GetProxyStatsAsync()
    {
        var proxies = await _context.ProxyConfigurations.ToListAsync();
        
        var total = proxies.Count;
        var active = proxies.Count(p => p.IsActive);
        var healthy = proxies.Count(p => p.IsActive && p.IsHealthy);
        var averageSuccessRate = proxies.Where(p => p.TotalRequests > 0)
                                       .Select(p => p.SuccessRate)
                                       .DefaultIfEmpty(0)
                                       .Average();

        return (total, active, healthy, Math.Round(averageSuccessRate, 2));
    }

    public async Task<IEnumerable<ProxyConfiguration>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _context.ProxyConfigurations
            .Where(p => ids.Contains(p.ProxyConfigurationId))
            .ToListAsync();
    }
}
