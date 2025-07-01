using Polly;
using System.Net;

namespace TechTicker.ScrapingWorker.Services.Interfaces;

/// <summary>
/// Interface for creating and managing retry policies using Polly
/// </summary>
public interface IRetryPolicyService
{
    /// <summary>
    /// Get the HTTP client retry policy with circuit breaker
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy();

    /// <summary>
    /// Get a generic retry policy for any operation
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    IAsyncPolicy<T> GetGenericRetryPolicy<T>();

    /// <summary>
    /// Get the timeout policy for individual requests
    /// </summary>
    IAsyncPolicy GetTimeoutPolicy();

    /// <summary>
    /// Get the overall timeout policy for entire operations
    /// </summary>
    IAsyncPolicy GetOverallTimeoutPolicy();

    /// <summary>
    /// Get the circuit breaker policy
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();

    /// <summary>
    /// Check if an HTTP status code should trigger a retry
    /// </summary>
    bool ShouldRetryForStatusCode(HttpStatusCode statusCode);

    /// <summary>
    /// Check if an exception should trigger a retry
    /// </summary>
    bool ShouldRetryForException(Exception exception);
}
