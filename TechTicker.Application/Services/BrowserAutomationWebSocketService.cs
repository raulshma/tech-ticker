using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing WebSocket connections and real-time communication for browser automation testing
/// </summary>
public class BrowserAutomationWebSocketService : IBrowserAutomationWebSocketService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BrowserAutomationWebSocketService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _sessionConnections;
    private readonly ConcurrentDictionary<string, string> _connectionSessions;

    // Event to notify API layer about broadcasts
    public static event Func<string, string, object, Task>? OnBroadcastRequested;

    public BrowserAutomationWebSocketService(
        IServiceProvider serviceProvider,
        ILogger<BrowserAutomationWebSocketService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sessionConnections = new ConcurrentDictionary<string, HashSet<string>>();
        _connectionSessions = new ConcurrentDictionary<string, string>();
    }

    public Task AddConnectionAsync(string sessionId, string connectionId)
    {
        try
        {
            _sessionConnections.AddOrUpdate(
                sessionId,
                new HashSet<string> { connectionId },
                (key, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(connectionId);
                        return existingSet;
                    }
                });

            _connectionSessions[connectionId] = sessionId;

            _logger.LogDebug("Added connection {ConnectionId} to session {SessionId}", 
                connectionId, sessionId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection {ConnectionId} to session {SessionId}", 
                connectionId, sessionId);
            return Task.CompletedTask;
        }
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        try
        {
            if (_connectionSessions.TryRemove(connectionId, out var sessionId))
            {
                if (_sessionConnections.TryGetValue(sessionId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);
                        if (connections.Count == 0)
                        {
                            _sessionConnections.TryRemove(sessionId, out _);
                        }
                    }
                }

                _logger.LogDebug("Removed connection {ConnectionId} from session {SessionId}", 
                    connectionId, sessionId);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId}", connectionId);
            return Task.CompletedTask;
        }
    }

    public async Task BroadcastBrowserStateAsync(string sessionId, BrowserStateUpdateDto update)
    {
        try
        {
            var message = new
            {
                Type = "browser_state",
                SessionId = sessionId,
                Data = update,
                Timestamp = DateTimeOffset.UtcNow
            };

            await BroadcastToSessionAsync(sessionId, "BrowserStateUpdate", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting browser state for session {SessionId}", sessionId);
        }
    }

    public async Task BroadcastLogEntryAsync(string sessionId, TestLogEntryDto logEntry)
    {
        try
        {
            var message = new
            {
                Type = "log_entry",
                SessionId = sessionId,
                Data = logEntry,
                Timestamp = DateTimeOffset.UtcNow
            };

            await BroadcastToSessionAsync(sessionId, "LogEntry", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting log entry for session {SessionId}", sessionId);
        }
    }

    public async Task BroadcastMetricsAsync(string sessionId, ExecutionMetricsDto metrics)
    {
        try
        {
            var message = new
            {
                Type = "performance_metrics",
                SessionId = sessionId,
                Data = metrics,
                Timestamp = DateTimeOffset.UtcNow
            };

            await BroadcastToSessionAsync(sessionId, "PerformanceMetrics", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting metrics for session {SessionId}", sessionId);
        }
    }

    public async Task BroadcastTestCompletedAsync(string sessionId, BrowserAutomationTestResultDto result)
    {
        try
        {
            var message = new
            {
                Type = "test_completed",
                SessionId = sessionId,
                Data = result,
                Timestamp = DateTimeOffset.UtcNow
            };

            await BroadcastToSessionAsync(sessionId, "TestCompleted", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting test completion for session {SessionId}", sessionId);
        }
    }

    public async Task BroadcastErrorAsync(string sessionId, TestErrorDto error)
    {
        try
        {
            var message = new
            {
                Type = "error",
                SessionId = sessionId,
                Data = error,
                Timestamp = DateTimeOffset.UtcNow
            };

            await BroadcastToSessionAsync(sessionId, "Error", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting error for session {SessionId}", sessionId);
        }
    }

    private async Task BroadcastToSessionAsync(string sessionId, string method, object data)
    {
        try
        {
            var groupName = GetSessionGroupName(sessionId);
            
            // Use event to notify API layer to handle the actual SignalR broadcasting
            if (OnBroadcastRequested != null)
            {
                await OnBroadcastRequested.Invoke(groupName, method, data);
                _logger.LogTrace("Successfully requested broadcast of {Method} to session group {GroupName}", method, groupName);
            }
            else
            {
                _logger.LogWarning("No broadcast handler available for {Method} to session {SessionId}", method, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting {Method} to session {SessionId}", method, sessionId);
        }
    }

    /// <summary>
    /// Get the SignalR group name for a test session
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <returns>Group name</returns>
    private static string GetSessionGroupName(string sessionId) => $"test-session-{sessionId}";
}

 