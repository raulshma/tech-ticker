using System.Text.Json;

namespace TechTicker.ReverseProxy.Middleware;

/// <summary>
/// Middleware for request validation and transformation in the API Gateway
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add request validation logic
        if (!await ValidateRequest(context))
        {
            return; // Response already written by validation
        }

        // Add standard headers for downstream services
        AddGatewayHeaders(context);

        await _next(context);
    }

    private async Task<bool> ValidateRequest(HttpContext context)
    {
        // Validate content type for POST/PUT requests
        if (IsContentRequest(context.Request.Method))
        {
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || 
                (!contentType.Contains("application/json") && !contentType.Contains("multipart/form-data")))
            {
                await WriteErrorResponse(context, 400, "Invalid content type. Expected application/json or multipart/form-data");
                return false;
            }
        }

        // Validate request size
        if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
        {
            await WriteErrorResponse(context, 413, "Request payload too large. Maximum size is 10MB");
            return false;
        }

        return true;
    }

    private static bool IsContentRequest(string method)
    {
        return method is "POST" or "PUT" or "PATCH";
    }

    private static void AddGatewayHeaders(HttpContext context)
    {
        // Add correlation ID if not present
        if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.Request.Headers.Append("X-Correlation-ID", Guid.NewGuid().ToString());
        }

        // Add gateway identifier
        context.Request.Headers.Append("X-API-Gateway", "TechTicker-Gateway");
        context.Request.Headers.Append("X-Gateway-Version", "1.0.0");
        context.Request.Headers.Append("X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

        // Add original client IP
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            context.Request.Headers.Append("X-Original-Client-IP", clientIp);
        }

        // Add user context if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers.Append("X-User-ID", userId);
            }

            var userRoles = context.User.FindAll("role").Select(c => c.Value);
            if (userRoles.Any())
            {
                context.Request.Headers.Append("X-User-Roles", string.Join(",", userRoles));
            }
        }
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        var response = new
        {
            error = GetErrorTitle(statusCode),
            message = message,
            statusCode = statusCode,
            requestId = context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static string GetErrorTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            413 => "Payload Too Large",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Error"
        };
    }
}
