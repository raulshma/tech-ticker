using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Health check service for Google AI integration
/// </summary>
public class GoogleAIHealthCheckService : IHealthCheck
{
    private readonly IAISelectorGenerationService _aiService;
    private readonly ProductDiscoveryOptions _options;
    private readonly ILogger<GoogleAIHealthCheckService> _logger;

    public GoogleAIHealthCheckService(
        IAISelectorGenerationService aiService,
        IOptions<ProductDiscoveryOptions> options,
        ILogger<GoogleAIHealthCheckService> logger)
    {
        _aiService = aiService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if API key is configured
            if (string.IsNullOrEmpty(_options.GoogleAI.ApiKey))
            {
                return HealthCheckResult.Unhealthy("Google AI API key is not configured");
            }

            // Test with minimal HTML content
            var testHtml = "<html><body><h1>Test Product</h1><span class='price'>$99.99</span></body></html>";
            var result = await _aiService.GenerateSelectorsAsync(testHtml, "test.com");

            if (result.IsSuccess)
            {
                _logger.LogInformation("Google AI health check passed");
                return HealthCheckResult.Healthy("Google AI is responding correctly");
            }
            else
            {
                _logger.LogWarning("Google AI health check failed: {Error}", result.ErrorMessage);
                return HealthCheckResult.Degraded($"Google AI responded with error: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google AI health check failed with exception");
            return HealthCheckResult.Unhealthy($"Google AI health check failed: {ex.Message}");
        }
    }
}
