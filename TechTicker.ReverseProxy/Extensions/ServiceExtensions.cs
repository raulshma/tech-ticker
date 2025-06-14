using Microsoft.Extensions.Diagnostics.HealthChecks;
using TechTicker.ReverseProxy.Configuration;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TechTicker.ReverseProxy.Extensions;

/// <summary>
/// Extension methods for configuring API Gateway services
/// </summary>
public static class ServiceExtensions
{    /// <summary>
    /// This method has been replaced by TechTicker shared authentication.
    /// Use services.AddTechTickerAuth(configuration) instead.
    /// </summary>
    [Obsolete("Use TechTicker shared authentication instead")]
    public static IServiceCollection AddApiGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        throw new NotSupportedException("Use services.AddTechTickerAuth(configuration) from TechTicker.Shared instead");
    }

    /// <summary>
    /// Configure authorization policies for the API Gateway
    /// </summary>
    public static IServiceCollection AddApiGatewayAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("authenticated", policy => 
                policy.RequireAuthenticatedUser())
            .AddPolicy("admin", policy => 
                policy.RequireAuthenticatedUser()
                       .RequireRole("Admin"))
            .AddPolicy("user", policy => 
                policy.RequireAuthenticatedUser()
                       .RequireRole("User", "Admin"));

        return services;
    }    /// <summary>
    /// Configure rate limiting for the API Gateway
    /// Note: Basic implementation - for production consider using Redis-based rate limiting
    /// </summary>
    public static IServiceCollection AddApiGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingSection = configuration.GetSection("RateLimiting");
        var defaultPolicy = rateLimitingSection.GetSection("DefaultPolicy");        services.AddRateLimiter(options =>
        {
            // Standard policy for general API access (renamed from "default" to avoid YARP conflicts)
            options.AddFixedWindowLimiter("standard", configureOptions =>
            {
                configureOptions.PermitLimit = defaultPolicy.GetValue<int>("PermitLimit", 100);
                configureOptions.Window = defaultPolicy.GetValue<TimeSpan>("Window", TimeSpan.FromMinutes(1));
                configureOptions.QueueLimit = defaultPolicy.GetValue<int>("QueueLimit", 50);
                configureOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Readonly policy for read operations (more permissive)
            options.AddFixedWindowLimiter("readonly", configureOptions =>
            {
                configureOptions.PermitLimit = 200; // Higher limit for readonly operations
                configureOptions.Window = TimeSpan.FromMinutes(1);
                configureOptions.QueueLimit = 100;
                configureOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Auth policy for authentication endpoints (more restrictive)
            options.AddFixedWindowLimiter("auth", configureOptions =>
            {
                configureOptions.PermitLimit = 20; // Lower limit for auth operations
                configureOptions.Window = TimeSpan.FromMinutes(1);
                configureOptions.QueueLimit = 10;
                configureOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Configure rejection response
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Rate limit exceeded. Please try again later.", token);
            };
        });
        
        return services;
    }

    /// <summary>
    /// Configure CORS for the API Gateway
    /// </summary>
    public static IServiceCollection AddApiGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors");
        var allowedOrigins = corsSettings.GetValue<string[]>("AllowedOrigins") ?? [];

        services.AddCors(options =>
        {
            if (allowedOrigins.Length > 0)
            {
                // Production CORS configuration with specific origins
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            }
            else
            {
                // Development CORS configuration (allow all)
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            }        });

        return services;
    }    /// <summary>
    /// Configure health checks for the API Gateway
    /// </summary>
    public static IServiceCollection AddApiGatewayHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the custom health check service
        services.AddScoped<ServiceHealthCheck>();
        services.AddHttpClient<ServiceHealthCheck>();
        
        var healthChecksBuilder = services.AddHealthChecks();

        // Add startup health check to ensure downstream services are ready
        healthChecksBuilder.AddCheck<StartupHealthCheck>("startup-ready", tags: new[] { "startup" });

        // Add health checks for downstream services
        var reverseProxyConfig = configuration.GetSection("ReverseProxy");
        var clusters = reverseProxyConfig.GetSection("Clusters");

        foreach (var cluster in clusters.GetChildren())
        {
            var destinations = cluster.GetSection("Destinations");
            foreach (var destination in destinations.GetChildren())
            {
                var address = destination.GetValue<string>("Address");
                if (!string.IsNullOrEmpty(address))
                {
                    // Convert Aspire address format to HTTP URL for health checks
                    var httpAddress = address.Replace("https+http://", "http://");
                    var healthCheckName = $"{cluster.Key}-{destination.Key}";
                      healthChecksBuilder.AddCheck<ServiceHealthCheck>(
                        healthCheckName,
                        tags: new[] { "downstream", cluster.Key });
                    
                    // Register the service URL for this health check
                    services.AddKeyedSingleton(healthCheckName, httpAddress);
                }
            }
        }

        return services;
    }
}

/// <summary>
/// Custom health check for downstream services
/// </summary>
public class ServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceHealthCheck> _logger;

    public ServiceHealthCheck(HttpClient httpClient, ILogger<ServiceHealthCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // For now, just return healthy since the actual health check URLs would be configured differently
        // In a production environment, this would check the actual downstream services
        try
        {
            _logger.LogDebug("Health check for {HealthCheckName}", context.Registration.Name);
            return Task.FromResult(HealthCheckResult.Healthy($"Health check {context.Registration.Name} is operational"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {HealthCheckName}", context.Registration.Name);
            return Task.FromResult(HealthCheckResult.Unhealthy($"Health check {context.Registration.Name} failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Startup health check that validates all downstream services are ready
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StartupHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public StartupHealthCheck(
        IServiceProvider serviceProvider, 
        IConfiguration configuration, 
        ILogger<StartupHealthCheck> logger,
        HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting downstream services health validation...");
            
            var results = new List<(string ServiceName, bool IsHealthy, string? ErrorMessage)>();
            var reverseProxyConfig = _configuration.GetSection("ReverseProxy");
            var clusters = reverseProxyConfig.GetSection("Clusters");

            foreach (var cluster in clusters.GetChildren())
            {
                var destinations = cluster.GetSection("Destinations");
                foreach (var destination in destinations.GetChildren())
                {
                    var address = destination.GetValue<string>("Address");
                    if (!string.IsNullOrEmpty(address))
                    {
                        var serviceName = $"{cluster.Key}-{destination.Key}";
                        var (isHealthy, errorMessage) = await CheckServiceHealth(address, serviceName, cancellationToken);
                        results.Add((serviceName, isHealthy, errorMessage));
                    }
                }
            }

            var unhealthyServices = results.Where(r => !r.IsHealthy).ToList();
            
            if (unhealthyServices.Any())
            {
                var errorMessages = string.Join("; ", unhealthyServices.Select(s => $"{s.ServiceName}: {s.ErrorMessage}"));
                _logger.LogWarning("Some downstream services are not ready: {ErrorMessages}", errorMessages);
                
                return HealthCheckResult.Degraded(
                    $"Waiting for {unhealthyServices.Count} downstream services to become ready: {errorMessages}",
                    data: new Dictionary<string, object>
                    {
                        ["total_services"] = results.Count,
                        ["healthy_services"] = results.Count - unhealthyServices.Count,
                        ["unhealthy_services"] = unhealthyServices.Count,
                        ["unhealthy_service_names"] = unhealthyServices.Select(s => s.ServiceName).ToArray()
                    });
            }

            _logger.LogInformation("All {ServiceCount} downstream services are healthy and ready", results.Count);
            return HealthCheckResult.Healthy(
                $"All {results.Count} downstream services are ready",
                data: new Dictionary<string, object>
                {
                    ["total_services"] = results.Count,
                    ["healthy_services"] = results.Count,
                    ["service_names"] = results.Select(s => s.ServiceName).ToArray()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate downstream services health");
            return HealthCheckResult.Unhealthy("Failed to validate downstream services", ex);
        }
    }

    private async Task<(bool IsHealthy, string? ErrorMessage)> CheckServiceHealth(
        string address, 
        string serviceName, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert Aspire address format to HTTP URL for health checks
            var httpAddress = address.Replace("https+http://", "http://");
            var healthUrl = $"{httpAddress}/health";
            
            _logger.LogDebug("Checking health of {ServiceName} at {HealthUrl}", serviceName, healthUrl);
            
            using var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Service {ServiceName} is healthy", serviceName);
                return (true, null);
            }
            else
            {
                var errorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                _logger.LogWarning("Service {ServiceName} health check failed: {ErrorMessage}", serviceName, errorMessage);
                return (false, errorMessage);
            }
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Connection failed: {ex.Message}";
            _logger.LogWarning("Service {ServiceName} health check failed: {ErrorMessage}", serviceName, errorMessage);
            return (false, errorMessage);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            var errorMessage = "Health check timeout";
            _logger.LogWarning("Service {ServiceName} health check timed out", serviceName);
            return (false, errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error checking health of {ServiceName}", serviceName);
            return (false, errorMessage);
        }
    }
}
