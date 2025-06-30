using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Constants;

namespace TechTicker.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time browser automation test communication
/// </summary>
[Authorize]
public class BrowserAutomationTestHub : Hub
{
    private readonly IBrowserAutomationWebSocketService _webSocketService;
    private readonly ILogger<BrowserAutomationTestHub> _logger;

    public BrowserAutomationTestHub(
        IBrowserAutomationWebSocketService webSocketService,
        ILogger<BrowserAutomationTestHub> logger)
    {
        _webSocketService = webSocketService;
        _logger = logger;
    }

    /// <summary>
    /// Join a test session group for real-time updates
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <returns>Task</returns>
    public async Task JoinTestSession(string sessionId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
            await _webSocketService.AddConnectionAsync(sessionId, Context.ConnectionId);
            
            _logger.LogInformation("Client {ConnectionId} joined test session {SessionId}", 
                Context.ConnectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining test session {SessionId} for connection {ConnectionId}", 
                sessionId, Context.ConnectionId);
            throw;
        }
    }

    /// <summary>
    /// Leave a test session group
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <returns>Task</returns>
    public async Task LeaveTestSession(string sessionId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
            
            _logger.LogInformation("Client {ConnectionId} left test session {SessionId}", 
                Context.ConnectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving test session {SessionId} for connection {ConnectionId}", 
                sessionId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    /// <param name="exception">Disconnection exception if any</param>
    /// <returns>Task</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _webSocketService.RemoveConnectionAsync(Context.ConnectionId);
            
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
            
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client {ConnectionId} disconnected with exception", 
                    Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection for {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle client connection
    /// </summary>
    /// <returns>Task</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to browser automation test hub", 
            Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Get the SignalR group name for a test session
    /// </summary>
    /// <param name="sessionId">Test session ID</param>
    /// <returns>Group name</returns>
    private static string GetSessionGroupName(string sessionId) => $"test-session-{sessionId}";
} 