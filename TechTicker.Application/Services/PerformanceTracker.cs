using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Implementation for tracking browser navigation and performance metrics
/// </summary>
public class PerformanceTracker : IPerformanceTracker
{
    private readonly ILogger<PerformanceTracker> _logger;
    private readonly NavigationMetricsDto _metrics;

    public PerformanceTracker(ILogger<PerformanceTracker> logger)
    {
        _logger = logger;
        _metrics = new NavigationMetricsDto();
    }

    public bool IsTracking { get; private set; }

    public void StartNavigationTracking()
    {
        if (IsTracking)
        {
            _logger.LogWarning("Navigation tracking is already active. Resetting metrics.");
            Reset();
        }

        _metrics.NavigationStartTime = DateTimeOffset.UtcNow;
        IsTracking = true;
        
        _logger.LogDebug("Started navigation tracking at {StartTime}", _metrics.NavigationStartTime);
    }

    public void StopNavigationTracking()
    {
        if (!IsTracking)
        {
            _logger.LogWarning("Cannot stop navigation tracking - no active tracking session");
            return;
        }

        _metrics.NavigationEndTime = DateTimeOffset.UtcNow;
        IsTracking = false;

        _logger.LogDebug("Stopped navigation tracking at {EndTime}. Total duration: {Duration}ms", 
            _metrics.NavigationEndTime, _metrics.TotalNavigationTimeMs);
    }

    public void RecordDomContentLoaded()
    {
        if (!IsTracking)
        {
            _logger.LogWarning("Cannot record DOM content loaded - no active tracking session");
            return;
        }

        _metrics.DomContentLoadedTime = DateTimeOffset.UtcNow;
        
        _logger.LogDebug("Recorded DOM content loaded at {Time}. Time from start: {Duration}ms", 
            _metrics.DomContentLoadedTime, _metrics.DomContentLoadedTimeMs);
    }

    public void RecordPageLoad()
    {
        if (!IsTracking)
        {
            _logger.LogWarning("Cannot record page load - no active tracking session");
            return;
        }

        _metrics.PageLoadTime = DateTimeOffset.UtcNow;
        
        _logger.LogDebug("Recorded page load at {Time}. Time from start: {Duration}ms", 
            _metrics.PageLoadTime, _metrics.PageLoadTimeMs);
    }

    public void RecordNetworkIdle()
    {
        if (!IsTracking)
        {
            _logger.LogWarning("Cannot record network idle - no active tracking session");
            return;
        }

        _metrics.NetworkIdleTime = DateTimeOffset.UtcNow;
        
        _logger.LogDebug("Recorded network idle at {Time}. Time from start: {Duration}ms", 
            _metrics.NetworkIdleTime, _metrics.NetworkIdleTimeMs);
    }

    public NavigationMetricsDto GetNavigationMetrics()
    {
        // Return a copy to prevent external modification
        return new NavigationMetricsDto
        {
            NavigationStartTime = _metrics.NavigationStartTime,
            DomContentLoadedTime = _metrics.DomContentLoadedTime,
            PageLoadTime = _metrics.PageLoadTime,
            NetworkIdleTime = _metrics.NetworkIdleTime,
            NavigationEndTime = _metrics.NavigationEndTime
        };
    }

    public void Reset()
    {
        _metrics.NavigationStartTime = null;
        _metrics.DomContentLoadedTime = null;
        _metrics.PageLoadTime = null;
        _metrics.NetworkIdleTime = null;
        _metrics.NavigationEndTime = null;
        IsTracking = false;

        _logger.LogDebug("Reset navigation tracking metrics");
    }
}
