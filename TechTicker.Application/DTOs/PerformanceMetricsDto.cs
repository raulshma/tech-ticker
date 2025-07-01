namespace TechTicker.Application.DTOs;

/// <summary>
/// Navigation performance metrics
/// </summary>
public class NavigationMetricsDto
{
    /// <summary>
    /// When navigation tracking started
    /// </summary>
    public DateTimeOffset? NavigationStartTime { get; set; }

    /// <summary>
    /// When DOM content was loaded
    /// </summary>
    public DateTimeOffset? DomContentLoadedTime { get; set; }

    /// <summary>
    /// When page load was complete
    /// </summary>
    public DateTimeOffset? PageLoadTime { get; set; }

    /// <summary>
    /// When network idle state was achieved
    /// </summary>
    public DateTimeOffset? NetworkIdleTime { get; set; }

    /// <summary>
    /// When navigation tracking stopped
    /// </summary>
    public DateTimeOffset? NavigationEndTime { get; set; }

    /// <summary>
    /// Total navigation time in milliseconds
    /// </summary>
    public int TotalNavigationTimeMs => NavigationStartTime.HasValue && NavigationEndTime.HasValue 
        ? (int)(NavigationEndTime.Value - NavigationStartTime.Value).TotalMilliseconds 
        : 0;

    /// <summary>
    /// Time to DOM content loaded in milliseconds
    /// </summary>
    public int DomContentLoadedTimeMs => NavigationStartTime.HasValue && DomContentLoadedTime.HasValue 
        ? (int)(DomContentLoadedTime.Value - NavigationStartTime.Value).TotalMilliseconds 
        : 0;

    /// <summary>
    /// Time to page load in milliseconds
    /// </summary>
    public int PageLoadTimeMs => NavigationStartTime.HasValue && PageLoadTime.HasValue 
        ? (int)(PageLoadTime.Value - NavigationStartTime.Value).TotalMilliseconds 
        : 0;

    /// <summary>
    /// Time to network idle in milliseconds
    /// </summary>
    public int NetworkIdleTimeMs => NavigationStartTime.HasValue && NetworkIdleTime.HasValue 
        ? (int)(NetworkIdleTime.Value - NavigationStartTime.Value).TotalMilliseconds 
        : 0;
}

/// <summary>
/// Network activity metrics
/// </summary>
public class NetworkMetricsDto
{
    /// <summary>
    /// Total number of network requests
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Number of successful requests (2xx status codes)
    /// </summary>
    public int SuccessfulRequestCount { get; set; }

    /// <summary>
    /// Number of failed requests (4xx, 5xx status codes)
    /// </summary>
    public int FailedRequestCount { get; set; }

    /// <summary>
    /// Total bytes received from all responses
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Total bytes sent in all requests
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Detailed information about individual requests
    /// </summary>
    public List<NetworkRequestDto> RequestDetails { get; set; } = new();

    /// <summary>
    /// When network monitoring started
    /// </summary>
    public DateTimeOffset? MonitoringStartTime { get; set; }

    /// <summary>
    /// When network monitoring stopped
    /// </summary>
    public DateTimeOffset? MonitoringEndTime { get; set; }

    /// <summary>
    /// Total monitoring duration in milliseconds
    /// </summary>
    public int TotalMonitoringTimeMs => MonitoringStartTime.HasValue && MonitoringEndTime.HasValue 
        ? (int)(MonitoringEndTime.Value - MonitoringStartTime.Value).TotalMilliseconds 
        : 0;
}
