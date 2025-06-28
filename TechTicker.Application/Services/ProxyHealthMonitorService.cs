using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Background service for monitoring proxy health
/// </summary>
public class ProxyHealthMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProxyHealthMonitorService> _logger;
    private readonly ProxyHealthMonitorConfiguration _config;

    public ProxyHealthMonitorService(
        IServiceProvider serviceProvider,
        ILogger<ProxyHealthMonitorService> logger,
        IOptions<ProxyHealthMonitorConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Proxy Health Monitor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync();
                await Task.Delay(TimeSpan.FromMinutes(_config.CheckIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during proxy health check cycle");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
            }
        }

        _logger.LogInformation("Proxy Health Monitor Service stopped");
    }

    private async Task PerformHealthCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();

        try
        {
            _logger.LogDebug("Starting proxy health check cycle");

            // Get proxies that need health checking
            var maxAge = TimeSpan.FromMinutes(_config.MaxAgeMinutes);
            var proxiesResult = await proxyService.GetProxiesForHealthCheckAsync(maxAge);

            if (!proxiesResult.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve proxies for health check: {Message}", proxiesResult.ErrorMessage);
                return;
            }

            var proxies = proxiesResult.Data!.ToList();
            if (!proxies.Any())
            {
                _logger.LogDebug("No proxies need health checking");
                return;
            }

            _logger.LogInformation("Performing health check on {Count} proxies", proxies.Count);

            var healthCheckTasks = proxies.Select(async proxy =>
            {
                try
                {
                    var testResult = await proxyService.TestProxyAsync(
                        proxy.ProxyConfigurationId, 
                        _config.TestUrl, 
                        _config.TimeoutSeconds);

                    if (testResult.IsSuccess)
                    {
                        _logger.LogDebug("Health check completed for proxy {Host}:{Port} - {Status}", 
                            proxy.Host, proxy.Port, testResult.Data!.IsHealthy ? "Healthy" : "Unhealthy");
                    }
                    else
                    {
                        _logger.LogWarning("Health check failed for proxy {Host}:{Port}: {Message}",
                            proxy.Host, proxy.Port, testResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing proxy {Host}:{Port}", proxy.Host, proxy.Port);
                }
            });

            await Task.WhenAll(healthCheckTasks);

            _logger.LogInformation("Completed health check cycle for {Count} proxies", proxies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during proxy health check");
        }
    }
}
