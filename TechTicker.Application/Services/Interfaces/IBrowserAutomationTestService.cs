using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for testing browser automation profiles with real-time feedback
/// </summary>
public interface IBrowserAutomationTestService
{
    /// <summary>
    /// Start a new browser automation test session
    /// </summary>
    /// <param name="request">Test request with profile and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test session information</returns>
    Task<Result<BrowserTestSessionDto>> StartTestSessionAsync(
        BrowserAutomationTestRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a running test session
    /// </summary>
    /// <param name="sessionId">Session ID to stop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results if available</returns>
    Task<Result<BrowserAutomationTestResultDto?>> StopTestSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a test session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session status information</returns>
    Task<Result<TestSessionStatusDto>> GetTestSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get test session results
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    Task<Result<BrowserAutomationTestResultDto>> GetTestSessionResultsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get test session details by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test session details</returns>
    Task<Result<BrowserTestSessionDto?>> GetTestSessionDetailsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current screenshot for a test session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Screenshot as base64 string</returns>
    Task<Result<string>> GetTestSessionScreenshotAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active test sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<Result<List<BrowserTestSessionDto>>> GetActiveTestSessionsAsync(
        CancellationToken cancellationToken = default);


}

/// <summary>
/// Service for managing real-time WebSocket connections for browser automation testing
/// </summary>
public interface IBrowserAutomationWebSocketService
{
    /// <summary>
    /// Add a WebSocket connection for a test session
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="connectionId">WebSocket connection ID</param>
    /// <returns>Task</returns>
    Task AddConnectionAsync(string sessionId, string connectionId);

    /// <summary>
    /// Remove a WebSocket connection
    /// </summary>
    /// <param name="connectionId">WebSocket connection ID</param>
    /// <returns>Task</returns>
    Task RemoveConnectionAsync(string connectionId);

    /// <summary>
    /// Broadcast browser state update to session connections
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="update">Browser state update</param>
    /// <returns>Task</returns>
    Task BroadcastBrowserStateAsync(string sessionId, BrowserStateUpdateDto update);

    /// <summary>
    /// Broadcast log entry to session connections
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="logEntry">Log entry</param>
    /// <returns>Task</returns>
    Task BroadcastLogEntryAsync(string sessionId, TestLogEntryDto logEntry);

    /// <summary>
    /// Broadcast performance metrics to session connections
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="metrics">Performance metrics</param>
    /// <returns>Task</returns>
    Task BroadcastMetricsAsync(string sessionId, ExecutionMetricsDto metrics);

    /// <summary>
    /// Broadcast test completion to session connections
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="result">Test result</param>
    /// <returns>Task</returns>
    Task BroadcastTestCompletedAsync(string sessionId, BrowserAutomationTestResultDto result);

    /// <summary>
    /// Broadcast error to session connections
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <param name="error">Error information</param>
    /// <returns>Task</returns>
    Task BroadcastErrorAsync(string sessionId, TestErrorDto error);
}