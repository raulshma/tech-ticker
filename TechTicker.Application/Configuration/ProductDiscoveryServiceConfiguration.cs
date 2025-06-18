using Microsoft.Extensions.DependencyInjection;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Configuration;

/// <summary>
/// Extension methods for configuring Product Discovery services
/// </summary>
public static class ProductDiscoveryServiceConfiguration
{
    /// <summary>
    /// Adds Product Discovery services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddProductDiscoveryServices(this IServiceCollection services)
    {
        // Core discovery services
        services.AddScoped<IProductDiscoveryService, ProductDiscoveryService>();
        services.AddScoped<IUrlAnalysisService, UrlAnalysisService>();
        services.AddScoped<ICategoryPredictionService, CategoryPredictionService>();
        services.AddScoped<IProductSimilarityService, ProductSimilarityService>();
        services.AddScoped<IDiscoveryWorkflowService, DiscoveryWorkflowService>();

        // Add HttpClient for URL analysis (configuration will be applied in the service constructor)
        services.AddHttpClient<UrlAnalysisService>();

        return services;
    }
}