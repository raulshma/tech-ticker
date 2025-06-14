using System.Diagnostics;

namespace TechTicker.ReverseProxy.Middleware;

/// <summary>
/// Middleware for enhanced request/response logging in the API Gateway
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        
        // Log incoming request
        _logger.LogInformation(
            "API Gateway Request {RequestId}: {Method} {Path} from {RemoteIP}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString());

        // Add custom headers for downstream services
        context.Request.Headers.Append("X-Gateway-Request-Id", requestId);
        context.Request.Headers.Append("X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToString("O"));
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            _logger.LogInformation(
                "API Gateway Response {RequestId}: {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
