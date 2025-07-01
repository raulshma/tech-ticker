using Microsoft.Playwright;
using TechTicker.Application.DTOs;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Interface for monitoring network activity during browser automation
/// </summary>
public interface INetworkMonitor
{
    /// <summary>
    /// Start monitoring network activity for a specific page
    /// </summary>
    /// <param name="page">The page to monitor</param>
    void StartMonitoring(IPage page);

    /// <summary>
    /// Stop monitoring network activity
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Get the collected network metrics
    /// </summary>
    /// <returns>Network activity metrics</returns>
    NetworkMetricsDto GetNetworkMetrics();

    /// <summary>
    /// Reset all network metrics
    /// </summary>
    void Reset();

    /// <summary>
    /// Check if currently monitoring network activity
    /// </summary>
    bool IsMonitoring { get; }
}
