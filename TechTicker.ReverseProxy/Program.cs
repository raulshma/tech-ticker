using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json;
using TechTicker.ServiceDefaults;
using TechTicker.Shared.Extensions;
using TechTicker.ReverseProxy.Middleware;
using TechTicker.ReverseProxy.Configuration;
using TechTicker.ReverseProxy.Extensions;
using TechTicker.ReverseProxy.Services;

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
        
        // Add HTTP logging        builder.Services.AddHttpLogging(o => { });
          // Configure API Gateway services using extension methods
        builder.Services.AddTechTickerAuth(builder.Configuration);
        builder.Services.AddApiGatewayAuthorization();
        builder.Services.AddApiGatewayRateLimiting(builder.Configuration);
        builder.Services.AddApiGatewayCors(builder.Configuration);        // Configure YARP Reverse Proxy
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
          // Add OpenAPI aggregation services
        builder.Services.AddHttpClient();
        
        // Configure HTTP client for development to accept self-signed certificates
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            });
        }
        
        builder.Services.Configure<OpenApiAggregationConfig>(
            builder.Configuration.GetSection("OpenApiAggregation"));
        builder.Services.AddScoped<IOpenApiAggregationService, OpenApiAggregationService>();
        
        // Add OpenAPI support
        builder.Services.AddOpenApi();
        
        // Configure settings        builder.Services.Configure<ApiGatewaySettings>(builder.Configuration);
        
        // Enhanced health checks with startup dependencies
        builder.Services.AddApiGatewayHealthChecks(builder.Configuration);
        
        // Add HTTP client and startup health check to the existing health checks
        builder.Services.AddHttpClient<StartupHealthCheck>();
        builder.Services.AddSingleton<StartupHealthCheck>();
        
        // Add background service for monitoring downstream health
        builder.Services.AddHostedService<DownstreamHealthMonitorService>();
        
        var app = builder.Build();        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // Custom endpoint to serve aggregated OpenAPI document
            app.MapGet("/openapi/v1.json", async (IOpenApiAggregationService aggregationService) =>
            {
                var document = await aggregationService.GetAggregatedOpenApiDocumentAsync();
                return Results.Json(document, options: new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            });

            // Configure Scalar API documentation with aggregated OpenAPI
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("TechTicker API Gateway Documentation")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithSidebar(true)
                    .WithModels(true)
                    .WithDownloadButton(true)
                    .WithSearchHotKey("KeyK")
                    .WithOpenApiRoutePattern("/openapi/v1.json")
                    .WithCustomCss("""
                        .scalar-app {
                            --scalar-font: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
                        }
                        .scalar-app .dark-mode {
                            --scalar-color-1: #1a1a1a;
                            --scalar-color-2: #2a2a2a;
                            --scalar-color-3: #3a3a3a;
                        }
                        .api-reference__header {
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        }
                        """);
            });
            
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
        
        // Enable CORS
        app.UseCors();
          // Enable rate limiting
        app.UseRateLimiter();
        
        // Enable authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Add response aggregation middleware
        app.UseMiddleware<ResponseAggregationMiddleware>();        // Health check endpoints with OpenAPI documentation
        app.MapHealthChecks("/health")
            .WithTags("Health")
            .WithSummary("General health check")
            .WithDescription("Returns the overall health status of the API Gateway and its dependencies");
            
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("startup") || check.Tags.Contains("ready")
        })
            .WithTags("Health")  
            .WithSummary("Readiness health check")
            .WithDescription("Returns readiness status indicating if the service is ready to accept requests. Includes validation of all downstream services.");
            
        app.MapHealthChecks("/health/live")
            .WithTags("Health")
            .WithSummary("Liveness health check") 
            .WithDescription("Returns liveness status indicating if the service is running and operational");

        // Downstream services health check endpoint
        app.MapHealthChecks("/health/downstream", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("downstream")
        })
            .WithTags("Health")
            .WithSummary("Downstream services health check")
            .WithDescription("Returns detailed health status of all configured downstream services");

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
        })
        .WithTags("Gateway")
        .WithSummary("Gateway status and configuration")
        .WithDescription("Returns detailed information about the API Gateway configuration, features, and downstream services")
        .Produces(200);
          // API Gateway documentation endpoint
        app.MapGet("/gateway/routes", (IConfiguration configuration) => 
        {
            var routes = new List<object>();
            var reverseProxyConfig = configuration.GetSection("ReverseProxy");
            var routesConfig = reverseProxyConfig.GetSection("Routes");
            var clustersConfig = reverseProxyConfig.GetSection("Clusters");
            
            foreach (var route in routesConfig.GetChildren())
            {
                var clusterId = route.GetValue<string>("ClusterId");
                if (string.IsNullOrEmpty(clusterId)) continue;
                
                var cluster = clustersConfig.GetSection(clusterId);
                var destination = cluster.GetSection("Destinations").GetChildren().FirstOrDefault();
                
                routes.Add(new
                {
                    RouteId = route.Key,
                    Path = route.GetValue<string>("Match:Path"),
                    ClusterId = clusterId,
                    DestinationAddress = destination?.GetValue<string>("Address"),
                    AuthPolicy = route.GetValue<string>("AuthorizationPolicy"),
                    RateLimit = route.GetValue<string>("RateLimiterPolicy"),
                    Transform = route.GetSection("Transforms").GetChildren().FirstOrDefault()?.GetValue<string>("PathPattern"),
                    Description = GetRouteDescription(route.Key)
                });
            }
            
            return new
            {
                ApiGateway = "TechTicker API Gateway",
                Version = "1.0.0",
                Documentation = "/scalar/v1",
                TotalRoutes = routes.Count,
                Routes = routes,
                GeneratedAt = DateTime.UtcNow
            };
        })
        .WithTags("Gateway")
        .WithSummary("API routes documentation")
        .WithDescription("Returns comprehensive information about all configured API routes, their targets, and policies")
        .Produces(200);
        
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
      private static string GetRouteDescription(string routeId)
    {
        return routeId switch
        {
            "categories-route" => "Product categories management - Browse and search product categories",
            "products-route" => "Product catalog operations - Search, filter, and retrieve product information",
            "mappings-route" => "Product-seller mapping configuration - Manage how products map to different seller platforms",
            "site-configs-route" => "Site configuration management - Configure scraping and seller integration settings",
            "auth-connect-route" => "Authentication endpoints - OAuth2/OpenID Connect authentication flow",
            "auth-legacy-route" => "Legacy authentication support - Backward compatibility for existing auth flows", 
            "users-route" => "User management operations - User profiles, preferences, and account management",
            "prices-route" => "Price history and tracking - Historical price data and price change notifications",
            "alerts-route" => "Price alert management - Configure and manage price monitoring alerts",
            _ => $"API route for {routeId.Replace("-route", "").Replace("-", " ")} operations"
        };
    }
}
