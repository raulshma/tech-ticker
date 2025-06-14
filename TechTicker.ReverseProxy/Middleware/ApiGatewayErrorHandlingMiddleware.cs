using System.Text.Json;

namespace TechTicker.ReverseProxy.Middleware;

/// <summary>
/// Middleware for handling and standardizing API Gateway errors
/// </summary>
public class ApiGatewayErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiGatewayErrorHandlingMiddleware> _logger;

    public ApiGatewayErrorHandlingMiddleware(RequestDelegate next, ILogger<ApiGatewayErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        
        _logger.LogError(exception, 
            "API Gateway Error {RequestId}: {Method} {Path} - {Message}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            exception.Message);

        var response = new
        {
            error = "Internal Server Error",
            message = "An error occurred while processing your request",
            requestId = requestId,
            timestamp = DateTimeOffset.UtcNow
        };

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
