using System.Text.Json;

namespace TechTicker.ReverseProxy.Middleware;

/// <summary>
/// Middleware for aggregating responses from multiple services and enhancing response metadata
/// </summary>
public class ResponseAggregationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseAggregationMiddleware> _logger;

    public ResponseAggregationMiddleware(RequestDelegate next, ILogger<ResponseAggregationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original response stream
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Add standard response headers
        AddGatewayResponseHeaders(context);

        // Handle response aggregation for specific paths
        await HandleResponseAggregation(context, originalBodyStream, responseBody);
    }

    private static void AddGatewayResponseHeaders(HttpContext context)
    {
        // Add API Gateway identifier
        context.Response.Headers.Append("X-API-Gateway", "TechTicker-Gateway");
        context.Response.Headers.Append("X-Gateway-Version", "1.0.0");
        context.Response.Headers.Append("X-Response-Time", DateTimeOffset.UtcNow.ToString("O"));
        
        // Add correlation ID from request
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? 
                           context.TraceIdentifier;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Add cache control for API responses
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            context.Response.Headers.Append("Pragma", "no-cache");
            context.Response.Headers.Append("Expires", "0");
        }
    }

    private async Task HandleResponseAggregation(HttpContext context, Stream originalBodyStream, MemoryStream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        
        // Check if this is a request that needs response aggregation
        if (ShouldAggregateResponse(context))
        {
            await AggregateResponse(context, originalBodyStream, responseBody);
        }
        else
        {
            // Just copy the response as-is
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private static bool ShouldAggregateResponse(HttpContext context)
    {
        // Define paths that need response aggregation
        var aggregationPaths = new[]
        {
            "/api/v1/products/{id}/details", // Hypothetical endpoint that might aggregate product + price data
            "/api/v1/dashboard/summary"      // Hypothetical dashboard endpoint
        };

        var path = context.Request.Path.Value?.ToLowerInvariant();
        return aggregationPaths.Any(p => path?.Contains(p.Split('{')[0]) == true);
    }

    private async Task AggregateResponse(HttpContext context, Stream originalBodyStream, MemoryStream responseBody)
    {
        try
        {
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            
            // For now, just enhance the existing response with metadata
            // In the future, this could combine data from multiple service calls
            var enhancedResponse = await EnhanceResponse(context, responseContent);
            
            await originalBodyStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(enhancedResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during response aggregation for {Path}", context.Request.Path);
            
            // Fall back to original response
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }    private Task<string> EnhanceResponse(HttpContext context, string originalResponse)
    {
        // Example enhancement: add metadata to the response
        try
        {
            var jsonDocument = JsonDocument.Parse(originalResponse);
            var responseObject = JsonSerializer.Deserialize<Dictionary<string, object>>(originalResponse);
            
            if (responseObject != null)
            {
                responseObject["_metadata"] = new
                {
                    gateway = "TechTicker-Gateway",
                    version = "1.0.0",
                    requestId = context.TraceIdentifier,
                    timestamp = DateTimeOffset.UtcNow,
                    path = context.Request.Path.Value
                };

                return Task.FromResult(JsonSerializer.Serialize(responseObject, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }
        }
        catch (JsonException)
        {
            // If it's not JSON, return as-is
        }

        return Task.FromResult(originalResponse);
    }
}
