using Microsoft.Extensions.Options;
using System.Text.Json;

namespace TechTicker.ReverseProxy.Services;

public interface IOpenApiAggregationService
{
    Task<object> GetAggregatedOpenApiDocumentAsync();
}

public class OpenApiAggregationConfig
{
    public List<ServiceConfig> Services { get; set; } = new();
}

public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string RoutePrefix { get; set; } = string.Empty;
}

/// <summary>
/// Service responsible for aggregating OpenAPI specifications from downstream services
/// </summary>
public class OpenApiAggregationService : IOpenApiAggregationService
{
    private readonly HttpClient _httpClient;
    private readonly OpenApiAggregationConfig _config;
    private readonly ILogger<OpenApiAggregationService> _logger;

    public OpenApiAggregationService(
        HttpClient httpClient,
        IOptions<OpenApiAggregationConfig> config,
        ILogger<OpenApiAggregationService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates OpenAPI specifications from all configured downstream services
    /// </summary>
    public async Task<object> GetAggregatedOpenApiDocumentAsync()
    {
        var allPaths = new Dictionary<string, object>();
        var allSchemas = new Dictionary<string, object>();

        foreach (var serviceConfig in _config.Services)
        {
            try
            {
                await AggregateServiceDocumentationAsync(allPaths, allSchemas, serviceConfig);
                _logger.LogInformation("Successfully aggregated OpenAPI spec from {ServiceName}", serviceConfig.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to aggregate OpenAPI spec from {ServiceName}", serviceConfig.Name);
            }
        }

        return new
        {
            openapi = "3.0.1",
            info = new
            {
                title = "TechTicker API Gateway",
                version = "v1",
                description = "Aggregated API documentation for all TechTicker services"
            },
            servers = new[] { new { url = "/" } },
            paths = allPaths,
            components = new
            {
                schemas = allSchemas,
                securitySchemes = new
                {
                    Bearer = new
                    {
                        type = "http",
                        scheme = "bearer",
                        bearerFormat = "JWT",
                        description = "Enter your JWT Bearer token"
                    }
                }
            },
            security = new[]
            {
                new { Bearer = new string[] { } }
            }
        };
    }

    private async Task AggregateServiceDocumentationAsync(
        Dictionary<string, object> allPaths, 
        Dictionary<string, object> allSchemas, 
        ServiceConfig serviceConfig)
    {
        var openApiUrl = $"{serviceConfig.BaseUrl.TrimEnd('/')}/openapi/v1.json";
        
        try
        {
            _logger.LogInformation("Fetching OpenAPI spec from {Url} for service {ServiceName}", 
                openApiUrl, serviceConfig.Name);

            var response = await _httpClient.GetAsync(openApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not fetch OpenAPI document from {Url}. Status: {Status}", 
                    openApiUrl, response.StatusCode);
                return;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var serviceDocument = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            // Add paths with service prefix
            if (serviceDocument.TryGetProperty("paths", out var pathsElement))
            {
                AddPathsToCollection(allPaths, pathsElement, serviceConfig);
            }

            // Add components/schemas
            if (serviceDocument.TryGetProperty("components", out var componentsElement) &&
                componentsElement.TryGetProperty("schemas", out var schemasElement))
            {
                AddSchemasToCollection(allSchemas, schemasElement, serviceConfig);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OpenAPI document from {Url}", openApiUrl);
        }
    }    private void AddPathsToCollection(Dictionary<string, object> allPaths, JsonElement pathsElement, ServiceConfig serviceConfig)
    {
        var servicePrefix = GetServicePrefix(serviceConfig);
        
        foreach (var pathProperty in pathsElement.EnumerateObject())
        {
            var originalPath = pathProperty.Name;
            
            // Transform the path to match the gateway's routing
            var transformedPath = TransformServicePath(originalPath, serviceConfig);
            
            try
            {
                // Parse the path operations and add service tag
                var pathItem = JsonSerializer.Deserialize<Dictionary<string, object>>(pathProperty.Value.GetRawText());
                
                if (pathItem != null)
                {
                    // Add service tag to all operations
                    foreach (var operation in pathItem.ToList())
                    {
                        if (operation.Value is JsonElement operationElement)
                        {
                            var operationDict = JsonSerializer.Deserialize<Dictionary<string, object>>(operationElement.GetRawText());
                            
                            if (operationDict != null)
                            {
                                // Add or update tags
                                if (!operationDict.ContainsKey("tags"))
                                {
                                    operationDict["tags"] = new[] { serviceConfig.Name };
                                }
                                else if (operationDict["tags"] is JsonElement tagsElement && tagsElement.ValueKind == JsonValueKind.Array)
                                {
                                    var existingTags = tagsElement.EnumerateArray()
                                        .Select(t => t.GetString())
                                        .Where(t => !string.IsNullOrEmpty(t))
                                        .ToList();
                                    
                                    if (!existingTags.Contains(serviceConfig.Name))
                                    {
                                        existingTags.Add(serviceConfig.Name);
                                    }
                                    
                                    operationDict["tags"] = existingTags.ToArray();
                                }
                                
                                // Update operationId to avoid conflicts
                                if (operationDict.ContainsKey("operationId"))
                                {
                                    var operationId = operationDict["operationId"]?.ToString();
                                    if (!string.IsNullOrEmpty(operationId) && !operationId.StartsWith($"{serviceConfig.Name}_"))
                                    {
                                        operationDict["operationId"] = $"{serviceConfig.Name}_{operationId}";
                                    }
                                }
                                
                                pathItem[operation.Key] = operationDict;
                            }
                        }
                    }
                    
                    allPaths[transformedPath] = pathItem;
                    
                    _logger.LogDebug("Mapped service path {OriginalPath} to gateway path {TransformedPath} for service {ServiceName}", 
                        originalPath, transformedPath, serviceConfig.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process path {Path} from service {ServiceName}", 
                    originalPath, serviceConfig.Name);
            }
        }
    }

    private void AddSchemasToCollection(Dictionary<string, object> allSchemas, JsonElement schemasElement, ServiceConfig serviceConfig)
    {
        foreach (var schemaProperty in schemasElement.EnumerateObject())
        {
            try
            {
                var schemaName = $"{serviceConfig.Name}_{schemaProperty.Name}";
                var schemaContent = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaProperty.Value.GetRawText());
                
                if (schemaContent != null)
                {
                    allSchemas[schemaName] = schemaContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process schema {SchemaName} from service {ServiceName}", 
                    schemaProperty.Name, serviceConfig.Name);
            }
        }
    }
    
    private string GetServicePrefix(ServiceConfig serviceConfig)
    {
        if (!string.IsNullOrEmpty(serviceConfig.RoutePrefix))
        {
            return serviceConfig.RoutePrefix.StartsWith('/') ? serviceConfig.RoutePrefix : $"/{serviceConfig.RoutePrefix}";
        }
        
        return $"/api/v1/{serviceConfig.Name.ToLowerInvariant()}";
    }
    
    /// <summary>
    /// Transforms a service's internal path to the gateway's exposed path
    /// </summary>
    private string TransformServicePath(string originalPath, ServiceConfig serviceConfig)
    {
        // If no route prefix is configured, use the default
        if (string.IsNullOrEmpty(serviceConfig.RoutePrefix))
        {
            return $"/api/v1/{serviceConfig.Name.ToLowerInvariant()}{originalPath}";
        }
        
        // Handle common service path patterns and transform them to gateway paths
        var normalizedPath = originalPath.TrimStart('/');
        var routePrefix = serviceConfig.RoutePrefix.TrimEnd('/');
          // Common transformations based on service naming patterns
        var transformedPath = serviceConfig.Name.ToLowerInvariant() switch
        {
            "productservice" => TransformProductServicePath(normalizedPath, routePrefix),
            "categoryservice" => TransformCategoryServicePath(normalizedPath, routePrefix),
            "userservice" => TransformUserServicePath(normalizedPath, routePrefix),
            "pricehistoryservice" => TransformPriceHistoryServicePath(normalizedPath, routePrefix),
            "productsellermappingservice" => TransformMappingServicePath(normalizedPath, routePrefix),
            "siteconfigservice" => TransformSiteConfigServicePath(normalizedPath, routePrefix),
            _ => TransformGenericServicePath(normalizedPath, routePrefix)
        };
        
        // Ensure path starts with /
        return transformedPath.StartsWith('/') ? transformedPath : $"/{transformedPath}";
    }
    
    private string TransformProductServicePath(string path, string routePrefix)
    {
        // Transform Product service paths
        // /api/Product -> /api/v1/products/api/Product
        // /api/categories -> /api/v1/categories/{**catch-all}
        
        if (path.StartsWith("api/Product", StringComparison.OrdinalIgnoreCase))
        {
            // Replace the service's /api/Product with gateway's /api/v1/products
            return path.Replace("api/Product", "api/v1/products", StringComparison.OrdinalIgnoreCase);
        }
        
        if (path.StartsWith("api/categories", StringComparison.OrdinalIgnoreCase))
        {
            // Map to gateway's categories route
            return path.Replace("api/categories", "api/v1/categories", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformCategoryServicePath(string path, string routePrefix)
    {
        // Transform Category service paths (same as Product service for categories)
        // /api/categories -> /api/v1/categories
        
        if (path.StartsWith("api/categories", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/categories", "api/v1/categories", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformUserServicePath(string path, string routePrefix)
    {
        // Transform User service paths
        // /api/users -> /api/v1/users
        // /connect/* -> /api/v1/auth/connect/*
        
        if (path.StartsWith("connect/", StringComparison.OrdinalIgnoreCase))
        {
            return $"api/v1/auth/{path}";
        }
        
        if (path.StartsWith("api/users", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/users", "api/v1/users", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformPriceHistoryServicePath(string path, string routePrefix)
    {
        // Transform Price History service paths
        // /api/prices -> /api/v1/prices
        
        if (path.StartsWith("api/prices", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/prices", "api/v1/prices", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformMappingServicePath(string path, string routePrefix)
    {
        // Transform Product Seller Mapping service paths
        // /api/mappings -> /api/v1/mappings
        // /api/site-configs -> /api/v1/site-configs
        
        if (path.StartsWith("api/mappings", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/mappings", "api/v1/mappings", StringComparison.OrdinalIgnoreCase);
        }
        
        if (path.StartsWith("api/site-configs", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/site-configs", "api/v1/site-configs", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformSiteConfigServicePath(string path, string routePrefix)
    {
        // Transform Site Config service paths
        // /api/site-configs -> /api/v1/site-configs
        
        if (path.StartsWith("api/site-configs", StringComparison.OrdinalIgnoreCase))
        {
            return path.Replace("api/site-configs", "api/v1/site-configs", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default mapping
        return $"{routePrefix}/{path}";
    }
    
    private string TransformGenericServicePath(string path, string routePrefix)
    {
        // Generic transformation for unknown services
        return $"{routePrefix}/{path}";
    }
}
