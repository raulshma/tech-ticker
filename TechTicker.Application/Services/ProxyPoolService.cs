using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service implementation for managing proxy pool for scraping operations
/// </summary>
public class ProxyPoolService : IProxyPoolService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProxyPoolService> _logger;
    private readonly ProxyPoolConfiguration _config;
    private readonly object _lockObject = new();
    private readonly Random _random = new();

    private const string PROXY_POOL_CACHE_KEY = "proxy_pool";
    private const string PROXY_POOL_STATS_CACHE_KEY = "proxy_pool_stats";

    public ProxyPoolService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<ProxyPoolService> logger,
        IOptions<ProxyPoolConfiguration> config)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public bool IsProxyPoolEnabled => _config.Enabled;

    public async Task<ProxyConfiguration?> GetNextProxyAsync(string? proxyType = null)
    {
        if (!IsProxyPoolEnabled)
        {
            _logger.LogDebug("Proxy pool is disabled");
            return null;
        }

        try
        {
            var availableProxies = await GetAvailableProxiesAsync(proxyType);
            if (!availableProxies.Any())
            {
                _logger.LogWarning("No available proxies found for type: {ProxyType}", proxyType ?? "any");
                return null;
            }

            var selectedProxy = SelectProxyByStrategy(availableProxies);
            _logger.LogDebug("Selected proxy {ProxyId} ({Host}:{Port}) for scraping", 
                selectedProxy.ProxyConfigurationId, selectedProxy.Host, selectedProxy.Port);

            return selectedProxy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next proxy from pool");
            return null;
        }
    }

    public async Task<ProxyConfiguration?> GetProxyByIdAsync(Guid proxyId)
    {
        try
        {
            return await _unitOfWork.ProxyConfigurations.GetByIdAsync(proxyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy {ProxyId}", proxyId);
            return null;
        }
    }

    public async Task RecordProxySuccessAsync(Guid proxyId, int responseTimeMs)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(proxyId);
            if (proxy != null)
            {
                proxy.RecordSuccess();
                _unitOfWork.ProxyConfigurations.Update(proxy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Recorded success for proxy {ProxyId} with response time {ResponseTime}ms", 
                    proxyId, responseTimeMs);

                // Invalidate cache to refresh pool
                _cache.Remove(PROXY_POOL_CACHE_KEY);
                _cache.Remove(PROXY_POOL_STATS_CACHE_KEY);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording proxy success for {ProxyId}", proxyId);
        }
    }

    public async Task RecordProxyFailureAsync(Guid proxyId, string? errorMessage = null, string? errorCode = null)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(proxyId);
            if (proxy != null)
            {
                proxy.RecordFailure(errorMessage, errorCode);
                _unitOfWork.ProxyConfigurations.Update(proxy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Recorded failure for proxy {ProxyId}: {ErrorMessage}", 
                    proxyId, errorMessage ?? "Unknown error");

                // Invalidate cache to refresh pool
                _cache.Remove(PROXY_POOL_CACHE_KEY);
                _cache.Remove(PROXY_POOL_STATS_CACHE_KEY);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording proxy failure for {ProxyId}", proxyId);
        }
    }

    public async Task<ProxyPoolStatsDto> GetPoolStatsAsync()
    {
        try
        {
            return await _cache.GetOrCreateAsync(PROXY_POOL_STATS_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.StatsCacheMinutes);

                var (total, active, healthy, averageSuccessRate) = await _unitOfWork.ProxyConfigurations.GetProxyStatsAsync();
                var allProxies = await _unitOfWork.ProxyConfigurations.GetActiveProxiesAsync();

                return new ProxyPoolStatsDto
                {
                    TotalProxies = total,
                    AvailableProxies = active,
                    HealthyProxies = healthy,
                    AverageSuccessRate = averageSuccessRate,
                    LastRefreshed = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    ProxiesByType = allProxies.GroupBy(p => p.ProxyType)
                                           .ToDictionary(g => g.Key, g => g.Count())
                };
            }) ?? new ProxyPoolStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy pool stats");
            return new ProxyPoolStatsDto();
        }
    }

    public async Task RefreshPoolAsync()
    {
        try
        {
            _cache.Remove(PROXY_POOL_CACHE_KEY);
            _cache.Remove(PROXY_POOL_STATS_CACHE_KEY);
            
            // Pre-load the cache
            await GetAvailableProxiesAsync();
            
            _logger.LogInformation("Proxy pool cache refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing proxy pool");
        }
    }

    private async Task<List<ProxyConfiguration>> GetAvailableProxiesAsync(string? proxyType = null)
    {
        var cacheKey = $"{PROXY_POOL_CACHE_KEY}_{proxyType ?? "all"}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.CacheMinutes);

            IEnumerable<ProxyConfiguration> proxies;
            
            if (!string.IsNullOrEmpty(proxyType))
            {
                proxies = await _unitOfWork.ProxyConfigurations.GetProxiesByTypeAsync(proxyType);
            }
            else
            {
                proxies = await _unitOfWork.ProxyConfigurations.GetHealthyProxiesAsync();
            }

            // Filter out proxies with too many consecutive failures
            var availableProxies = proxies.Where(p => 
                p.IsActive && 
                p.IsHealthy && 
                p.ConsecutiveFailures < _config.MaxConsecutiveFailures)
                .ToList();

            _logger.LogDebug("Found {Count} available proxies for type: {ProxyType}", 
                availableProxies.Count, proxyType ?? "any");

            return availableProxies;
        }) ?? new List<ProxyConfiguration>();
    }

    private ProxyConfiguration SelectProxyByStrategy(List<ProxyConfiguration> proxies)
    {
        return _config.SelectionStrategy.ToUpper() switch
        {
            "ROUND_ROBIN" => SelectRoundRobin(proxies),
            "LEAST_USED" => SelectLeastUsed(proxies),
            "BEST_SUCCESS_RATE" => SelectBestSuccessRate(proxies),
            "RANDOM" => SelectRandom(proxies),
            _ => SelectRandom(proxies) // Default fallback
        };
    }

    private ProxyConfiguration SelectRoundRobin(List<ProxyConfiguration> proxies)
    {
        lock (_lockObject)
        {
            // Simple round-robin based on proxy ID hash
            var sortedProxies = proxies.OrderBy(p => p.ProxyConfigurationId).ToList();
            var index = (int)(DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerSecond) % sortedProxies.Count;
            return sortedProxies[index];
        }
    }

    private ProxyConfiguration SelectLeastUsed(List<ProxyConfiguration> proxies)
    {
        return proxies.OrderBy(p => p.TotalRequests)
                     .ThenByDescending(p => p.SuccessRate)
                     .First();
    }

    private ProxyConfiguration SelectBestSuccessRate(List<ProxyConfiguration> proxies)
    {
        return proxies.OrderByDescending(p => p.SuccessRate)
                     .ThenBy(p => p.ConsecutiveFailures)
                     .ThenByDescending(p => p.LastUsedAt ?? DateTimeOffset.MinValue)
                     .First();
    }

    private ProxyConfiguration SelectRandom(List<ProxyConfiguration> proxies)
    {
        var index = _random.Next(proxies.Count);
        return proxies[index];
    }
}
