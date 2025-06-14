
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using TechTicker.ServiceDefaults;
using TechTicker.Shared.Extensions;
using TechTicker.ReverseProxy.Middleware;
using TechTicker.ReverseProxy.Configuration;
using TechTicker.ReverseProxy.Extensions;

namespace TechTicker.ReverseProxy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add service defaults (Aspire integration)
        builder.AddServiceDefaults();
        
        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();
        
        // Add HTTP logging
        builder.Services.AddHttpLogging(o => { });
        
        // Configure API Gateway services using extension methods
        builder.Services.AddApiGatewayAuthentication(builder.Configuration);
        builder.Services.AddApiGatewayAuthorization();
        builder.Services.AddApiGatewayRateLimiting(builder.Configuration);
        builder.Services.AddApiGatewayCors(builder.Configuration);
        builder.Services.AddApiGatewayHealthChecks(builder.Configuration);
        
        // Configure YARP Reverse Proxy
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
        
        // Add OpenAPI support
        builder.Services.AddOpenApi();
        
        // Configure settings
        builder.Services.Configure<ApiGatewaySettings>(builder.Configuration);
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
        }
        
        // Add TechTicker shared middleware (should be early in pipeline)
        app.UseTechTickerExceptionHandling();
        app.UseCorrelationId();
          // Add custom API Gateway error handling
        app.UseMiddleware<ApiGatewayErrorHandlingMiddleware>();
        
        // Add security middleware
        app.UseMiddleware<SecurityMiddleware>();
        
        // Add request validation middleware
        app.UseMiddleware<RequestValidationMiddleware>();
        
        // Add enhanced request logging middleware
        app.UseMiddleware<RequestLoggingMiddleware>();
          // Add HTTP logging
        app.UseHttpLogging();
        
        // Enable CORS
        app.UseCors();
        
        // Enable rate limiting
        // Rate limiting (currently placeholder - TODO: implement proper rate limiting)
        // app.UseRateLimiter();
        
        // Enable authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Add response aggregation middleware
        app.UseMiddleware<ResponseAggregationMiddleware>();
        
        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");
        
        // API Gateway info endpoint
        app.MapGet("/", () => new
        {
            Service = "TechTicker API Gateway",
            Version = "1.0.0",
            Environment = app.Environment.EnvironmentName,
            Timestamp = DateTime.UtcNow,
            Description = "Single entry point for all TechTicker API requests with authentication, rate limiting, and request routing"
        }).ExcludeFromDescription();
        
        // API Gateway status endpoint with detailed information
        app.MapGet("/gateway/status", (IConfiguration configuration) => new
        {
            Status = "Healthy",
            Gateway = new
            {
                Name = "TechTicker API Gateway",
                Version = "1.0.0",
                Environment = app.Environment.EnvironmentName
            },
            Features = new
            {
                Authentication = "JWT Bearer",
                RateLimiting = "Placeholder (TODO: implement)",
                CORS = "Enabled",
                RequestLogging = "Enabled",
                ResponseAggregation = "Available"
            },
            DownstreamServices = GetConfiguredServices(configuration),
            Timestamp = DateTime.UtcNow
        }).ExcludeFromDescription();
        
        // Map the reverse proxy
        app.MapReverseProxy();
        
        app.Run();
    }
    
    private static object GetConfiguredServices(IConfiguration configuration)
    {
        var services = new List<object>();
        var reverseProxyConfig = configuration.GetSection("ReverseProxy");
        var clusters = reverseProxyConfig.GetSection("Clusters");
        
        foreach (var cluster in clusters.GetChildren())
        {
            var destinations = cluster.GetSection("Destinations");
            var destinationInfo = destinations.GetChildren().Select(d => new
            {
                Name = d.Key,
                Address = d.GetValue<string>("Address")
            }).ToList();
            
            services.Add(new
            {
                ClusterName = cluster.Key,
                Destinations = destinationInfo
            });
        }
        
        return services;
    }
}
