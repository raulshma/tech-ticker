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

        // Add HttpClient for URL analysis
        services.AddHttpClient<UrlAnalysisService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        });

        return services;
    }
}