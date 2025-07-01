using System.ComponentModel.DataAnnotations;

namespace TechTicker.ScrapingWorker.Configuration;

/// <summary>
/// Configuration for retry policies using Polly
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts in milliseconds
    /// </summary>
    [Range(100, 60000)]
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay between retry attempts in milliseconds
    /// </summary>
    [Range(1000, 300000)]
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    [Range(1.0, 5.0)]
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Timeout configuration for individual requests
    /// </summary>
    public TimeoutConfiguration Timeout { get; set; } = new();

    /// <summary>
    /// HTTP status codes that should trigger a retry
    /// </summary>
    public List<int> RetryableHttpStatusCodes { get; set; } = new()
    {
        408, // Request Timeout
        429, // Too Many Requests
        500, // Internal Server Error
        502, // Bad Gateway
        503, // Service Unavailable
        504  // Gateway Timeout
    };

    /// <summary>
    /// Exception types that should trigger a retry
    /// </summary>
    public List<string> RetryableExceptionTypes { get; set; } = new()
    {
        nameof(HttpRequestException),
        nameof(TaskCanceledException)
    };
}

/// <summary>
/// Circuit breaker configuration
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit
    /// </summary>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Minimum throughput (requests) required before circuit breaker can open
    /// </summary>
    [Range(1, 1000)]
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Time to wait before attempting to close the circuit (in seconds)
    /// </summary>
    [Range(10, 3600)]
    public int DurationOfBreakSeconds { get; set; } = 60;

    /// <summary>
    /// Failure ratio threshold (0.0 to 1.0) to open the circuit
    /// </summary>
    [Range(0.1, 1.0)]
    public double FailureRatio { get; set; } = 0.6;

    /// <summary>
    /// Sampling duration for failure ratio calculation (in seconds)
    /// </summary>
    [Range(10, 3600)]
    public int SamplingDurationSeconds { get; set; } = 30;
}

/// <summary>
/// Timeout configuration
/// </summary>
public class TimeoutConfiguration
{
    /// <summary>
    /// Overall timeout for the entire operation (including retries) in seconds
    /// </summary>
    [Range(10, 600)]
    public int OverallTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Timeout for individual HTTP requests in seconds
    /// </summary>
    [Range(5, 120)]
    public int RequestTimeoutSeconds { get; set; } = 30;
}
