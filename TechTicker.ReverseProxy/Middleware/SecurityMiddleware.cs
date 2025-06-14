namespace TechTicker.ReverseProxy.Middleware;

/// <summary>
/// Middleware for enhanced security features in the API Gateway
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add comprehensive security headers
        AddSecurityHeaders(context);

        // Validate and sanitize request
        if (!await ValidateSecureRequest(context))
        {
            return; // Response already written
        }

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Security headers
        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("X-XSS-Protection", "1; mode=block");
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.Append("X-Permitted-Cross-Domain-Policies", "none");
        headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';");
        
        // Remove server information
        headers.Remove("Server");
        headers.Append("Server", "TechTicker-Gateway");

        // API-specific headers
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
        }
    }

    private async Task<bool> ValidateSecureRequest(HttpContext context)
    {
        // Check for suspicious patterns in URL
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(path))
        {
            // Check for common attack patterns
            var suspiciousPatterns = new[]
            {
                "../", "..\\", "%2e%2e", "%2e%2e%2f", "%2e%2e%5c",
                "<script", "javascript:", "vbscript:", "onload=", "onerror=",
                "union select", "drop table", "exec(", "eval(",
                "/etc/passwd", "/proc/", "cmd.exe", "powershell"
            };

            if (suspiciousPatterns.Any(pattern => path.Contains(pattern)))
            {
                _logger.LogWarning("Suspicious request pattern detected from {RemoteIp}: {Path}", 
                    context.Connection.RemoteIpAddress, path);
                
                await WriteSecurityErrorResponse(context, 400, "Invalid request format");
                return false;
            }
        }

        // Validate user agent (basic bot detection)
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(userAgent) && context.Request.Path.StartsWithSegments("/api"))
        {
            _logger.LogWarning("Request without User-Agent from {RemoteIp}", 
                context.Connection.RemoteIpAddress);
            
            await WriteSecurityErrorResponse(context, 400, "User-Agent header required");
            return false;
        }

        // Check for excessively long headers
        foreach (var header in context.Request.Headers)
        {
            if (header.Value.ToString().Length > 4096)
            {
                _logger.LogWarning("Excessively long header detected from {RemoteIp}: {HeaderName}", 
                    context.Connection.RemoteIpAddress, header.Key);
                
                await WriteSecurityErrorResponse(context, 400, "Request headers too long");
                return false;
            }
        }

        return true;
    }

    private async Task WriteSecurityErrorResponse(HttpContext context, int statusCode, string message)
    {
        var response = new
        {
            error = "Security Validation Failed",
            message = message,
            statusCode = statusCode,
            requestId = context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(response);
    }
}
