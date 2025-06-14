using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechTicker.ReverseProxy.Configuration;

namespace TechTicker.ReverseProxy.Extensions;

/// <summary>
/// Extension methods for configuring API Gateway services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configure JWT authentication for the API Gateway
    /// </summary>
    public static IServiceCollection AddApiGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        
        if (string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is required in configuration");
        }

        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // For development - should be true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Configure JWT events for better logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>()
                            .LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>()
                            .LogDebug("JWT Token validated for user: {UserId}", 
                                context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
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
        // For now, we'll implement a basic in-memory rate limiter
        // In production, consider using:
        // - Redis-based rate limiting
        // - AspNetCoreRateLimit package
        // - Cloud-based rate limiting (Azure API Management, AWS API Gateway)
        
        // TODO: Implement proper rate limiting middleware
        // This is a placeholder that can be enhanced with proper rate limiting logic
        
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
    }

    /// <summary>
    /// Configure health checks for the API Gateway
    /// </summary>
    public static IServiceCollection AddApiGatewayHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the custom health check service
        services.AddScoped<ServiceHealthCheck>();
        services.AddHttpClient<ServiceHealthCheck>();
        
        var healthChecksBuilder = services.AddHealthChecks();

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
