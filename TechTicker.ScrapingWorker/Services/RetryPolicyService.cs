using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using System.Net.Sockets;
using TechTicker.ScrapingWorker.Configuration;
using TechTicker.ScrapingWorker.Services.Interfaces;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for creating and managing retry policies using Polly
/// </summary>
public class RetryPolicyService : IRetryPolicyService
{
    private readonly ILogger<RetryPolicyService> _logger;
    private readonly RetryConfiguration _config;
    private readonly Random _jitterRandom = new();

    public RetryPolicyService(
        ILogger<RetryPolicyService> logger,
        IOptions<RetryConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Get the HTTP client retry policy with circuit breaker
    /// </summary>
    public IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        // Create retry policy
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<SocketException>()
            .OrResult<HttpResponseMessage>(r => ShouldRetryForStatusCode(r.StatusCode))
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetryAttempts,
                sleepDurationProvider: CalculateRetryDelay,
                onRetry: OnHttpRetry);

        // Create circuit breaker policy
        var circuitBreakerPolicy = GetCircuitBreakerPolicy();

        // Create timeout policy for individual requests
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(_config.Timeout.RequestTimeoutSeconds);

        // Combine policies: Timeout -> Retry -> Circuit Breaker
        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    /// <summary>
    /// Get a generic retry policy for any operation
    /// </summary>
    public IAsyncPolicy<T> GetGenericRetryPolicy<T>()
    {
        return Policy
            .Handle<Exception>(ShouldRetryForException)
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetryAttempts,
                sleepDurationProvider: CalculateRetryDelay,
                onRetry: (exception, delay, retryCount, context) => 
                {
                    _logger.LogWarning(
                        "Generic retry attempt {RetryCount}/{MaxRetries} after {Delay}ms delay. Exception: {ExceptionType} - {Message}",
                        retryCount, _config.MaxRetryAttempts, delay.TotalMilliseconds,
                        exception.GetType().Name, exception.Message);
                })
            .AsAsyncPolicy<T>();
    }

    /// <summary>
    /// Get the timeout policy for individual requests
    /// </summary>
    public IAsyncPolicy GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync(_config.Timeout.RequestTimeoutSeconds);
    }

    /// <summary>
    /// Get the overall timeout policy for entire operations
    /// </summary>
    public IAsyncPolicy GetOverallTimeoutPolicy()
    {
        return Policy.TimeoutAsync(_config.Timeout.OverallTimeoutSeconds);
    }

    /// <summary>
    /// Get the circuit breaker policy
    /// </summary>
    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<SocketException>()
            .OrResult<HttpResponseMessage>(r => ShouldRetryForStatusCode(r.StatusCode))
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _config.CircuitBreaker.FailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_config.CircuitBreaker.DurationOfBreakSeconds),
                onBreak: OnCircuitBreakerBreak,
                onReset: OnCircuitBreakerReset);
    }

    /// <summary>
    /// Check if an HTTP status code should trigger a retry
    /// </summary>
    public bool ShouldRetryForStatusCode(HttpStatusCode statusCode)
    {
        var statusCodeInt = (int)statusCode;
        return _config.RetryableHttpStatusCodes.Contains(statusCodeInt);
    }

    /// <summary>
    /// Check if an exception should trigger a retry
    /// </summary>
    public bool ShouldRetryForException(Exception exception)
    {
        var exceptionTypeName = exception.GetType().Name;
        
        // Check configured retryable exception types
        if (_config.RetryableExceptionTypes.Contains(exceptionTypeName))
            return true;

        // Special handling for timeout exceptions - check by type name to avoid version conflicts
        if (exception.GetType().Name == "TimeoutRejectedException")
            return true;

        // Special handling for TaskCanceledException that's actually a timeout
        if (exception is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested)
            return true;

        // Special handling for HttpRequestException with specific messages
        if (exception is HttpRequestException httpEx)
        {
            var message = httpEx.Message.ToLowerInvariant();
            if (message.Contains("timeout") || 
                message.Contains("connection") || 
                message.Contains("network") ||
                message.Contains("proxy tunnel"))
                return true;
        }

        return false;
    }

    private TimeSpan CalculateRetryDelay(int retryAttempt)
    {
        var delay = TimeSpan.FromMilliseconds(_config.BaseDelayMs * Math.Pow(_config.BackoffMultiplier, retryAttempt - 1));
        
        // Apply maximum delay limit
        if (delay.TotalMilliseconds > _config.MaxDelayMs)
            delay = TimeSpan.FromMilliseconds(_config.MaxDelayMs);

        // Add jitter if enabled
        if (_config.UseJitter)
        {
            var jitterMs = _jitterRandom.Next(0, (int)(delay.TotalMilliseconds * 0.1)); // 10% jitter
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        return delay;
    }

    private void OnHttpRetry(DelegateResult<HttpResponseMessage> outcome, TimeSpan delay, int retryCount, Context context)
    {
        var statusCode = outcome.Result?.StatusCode;
        var exceptionMessage = outcome.Exception?.Message;

        if (outcome.Exception != null)
        {
            _logger.LogWarning(
                "HTTP retry attempt {RetryCount}/{MaxRetries} after {Delay}ms delay. Exception: {ExceptionType} - {Message}",
                retryCount, _config.MaxRetryAttempts, delay.TotalMilliseconds, 
                outcome.Exception.GetType().Name, exceptionMessage);
        }
        else if (statusCode.HasValue)
        {
            _logger.LogWarning(
                "HTTP retry attempt {RetryCount}/{MaxRetries} after {Delay}ms delay. Status Code: {StatusCode}",
                retryCount, _config.MaxRetryAttempts, delay.TotalMilliseconds, statusCode);
        }
    }

    private void OnCircuitBreakerBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan duration, Context context)
    {
        _logger.LogError(
            "Circuit breaker opened for {Duration}s. Last exception: {Exception}",
            duration.TotalSeconds, delegateResult.Exception?.Message);
    }

    private void OnCircuitBreakerReset(Context context)
    {
        _logger.LogInformation("Circuit breaker reset - circuit is now closed");
    }
}
