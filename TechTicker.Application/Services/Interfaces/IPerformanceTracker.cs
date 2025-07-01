using TechTicker.Application.DTOs;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Interface for tracking browser navigation and performance metrics
/// </summary>
public interface IPerformanceTracker
{
    /// <summary>
    /// Start tracking navigation performance
    /// </summary>
    void StartNavigationTracking();

    /// <summary>
    /// Stop tracking navigation performance
    /// </summary>
    void StopNavigationTracking();

    /// <summary>
    /// Record DOM content loaded event
    /// </summary>
    void RecordDomContentLoaded();

    /// <summary>
    /// Record full page load event
    /// </summary>
    void RecordPageLoad();

    /// <summary>
    /// Record network idle state achieved
    /// </summary>
    void RecordNetworkIdle();

    /// <summary>
    /// Get the collected navigation metrics
    /// </summary>
    /// <returns>Navigation performance metrics</returns>
    NavigationMetricsDto GetNavigationMetrics();

    /// <summary>
    /// Reset all tracking metrics
    /// </summary>
    void Reset();

    /// <summary>
    /// Check if currently tracking navigation
    /// </summary>
    bool IsTracking { get; }
}
