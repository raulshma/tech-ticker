using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TechTicker.ReverseProxy.Services;

/// <summary>
/// Background service that monitors the health of downstream services
/// and provides early warning for potential issues
/// </summary>
public class DownstreamHealthMonitorService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<DownstreamHealthMonitorService> _logger;
    private readonly TimeSpan _checkInterval;

    public DownstreamHealthMonitorService(
        HealthCheckService healthCheckService,
        ILogger<DownstreamHealthMonitorService> logger,
        IConfiguration configuration)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _checkInterval = configuration.GetValue<TimeSpan>("HealthCheck:MonitorInterval", TimeSpan.FromMinutes(2));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Downstream Health Monitor Service started with interval: {Interval}", _checkInterval);

        // Wait a bit before starting to allow services to start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorDownstreamServices(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring downstream services");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Downstream Health Monitor Service stopped");
    }

    private async Task MonitorDownstreamServices(CancellationToken cancellationToken)
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync(
                check => check.Tags.Contains("downstream"), 
                cancellationToken);

            var totalChecks = healthReport.Entries.Count;
            var healthyChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Healthy);
            var degradedChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Degraded);
            var unhealthyChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy);

            if (healthReport.Status == HealthStatus.Healthy)
            {
                _logger.LogDebug("All {TotalChecks} downstream services are healthy", totalChecks);
            }
            else if (healthReport.Status == HealthStatus.Degraded)
            {
                _logger.LogWarning(
                    "Some downstream services are degraded. Healthy: {Healthy}, Degraded: {Degraded}, Unhealthy: {Unhealthy}",
                    healthyChecks, degradedChecks, unhealthyChecks);

                LogUnhealthyServices(healthReport);
            }
            else
            {
                _logger.LogError(
                    "Critical: Some downstream services are unhealthy. Healthy: {Healthy}, Degraded: {Degraded}, Unhealthy: {Unhealthy}",
                    healthyChecks, degradedChecks, unhealthyChecks);

                LogUnhealthyServices(healthReport);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check downstream services health");
        }
    }

    private void LogUnhealthyServices(HealthReport healthReport)
    {
        var unhealthyServices = healthReport.Entries
            .Where(e => e.Value.Status != HealthStatus.Healthy)
            .ToList();

        foreach (var (serviceName, healthReportEntry) in unhealthyServices)
        {
            var logLevel = healthReportEntry.Status == HealthStatus.Degraded ? LogLevel.Warning : LogLevel.Error;
            
            _logger.Log(logLevel, 
                "Service {ServiceName} is {Status}: {Description}. Duration: {Duration}ms", 
                serviceName, 
                healthReportEntry.Status, 
                healthReportEntry.Description,
                healthReportEntry.Duration.TotalMilliseconds);

            if (healthReportEntry.Exception != null)
            {
                _logger.Log(logLevel, healthReportEntry.Exception, 
                    "Service {ServiceName} health check exception", serviceName);
            }

            if (healthReportEntry.Data.Any())
            {
                foreach (var (key, value) in healthReportEntry.Data)
                {
                    _logger.LogDebug("Service {ServiceName} health data - {Key}: {Value}", 
                        serviceName, key, value);
                }
            }
        }
    }
}
