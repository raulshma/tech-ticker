namespace TechTicker.ReverseProxy.Configuration;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Rate limiting configuration settings
/// </summary>
public class RateLimitSettings
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public int QueueLimit { get; set; } = 50;
}

/// <summary>
/// CORS configuration settings
/// </summary>
public class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH"];
    public string[] AllowedHeaders { get; set; } = ["*"];
    public bool AllowCredentials { get; set; } = true;
}

/// <summary>
/// Service discovery settings for downstream services
/// </summary>
public class ServiceDiscoverySettings
{
    public Dictionary<string, ServiceEndpoint> Services { get; set; } = new();
}

/// <summary>
/// Individual service endpoint configuration
/// </summary>
public class ServiceEndpoint
{
    public string BaseUrl { get; set; } = string.Empty;
    public string HealthCheckPath { get; set; } = "/health";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool RequiresAuthentication { get; set; } = true;
}

/// <summary>
/// Main API Gateway configuration settings
/// </summary>
public class ApiGatewaySettings
{
    public JwtSettings Jwt { get; set; } = new();
    public RateLimitSettings RateLimiting { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
    public ServiceDiscoverySettings ServiceDiscovery { get; set; } = new();
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseAggregation { get; set; } = false;
    public bool EnableDetailedHealthChecks { get; set; } = true;
}
