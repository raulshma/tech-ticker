using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

public interface IIntegrationsAndFeaturesService
{
    /// <summary>
    /// Gets the complete overview of all features and integrations
    /// </summary>
    /// <param name="userId">Current user ID for personalized status</param>
    /// <returns>Complete integrations and features overview</returns>
    Task<Result<IntegrationsAndFeaturesDto>> GetIntegrationsAndFeaturesAsync(Guid userId);

    /// <summary>
    /// Gets all available features with their current status
    /// </summary>
    /// <param name="userId">Current user ID for personalized status</param>
    /// <returns>List of features with status</returns>
    Task<Result<List<FeatureDto>>> GetFeaturesAsync(Guid userId);

    /// <summary>
    /// Gets all integrations with their current status
    /// </summary>
    /// <returns>List of integrations with status</returns>
    Task<Result<List<IntegrationDto>>> GetIntegrationsAsync();

    /// <summary>
    /// Gets system health overview
    /// </summary>
    /// <returns>System health information</returns>
    Task<Result<SystemHealthDto>> GetSystemHealthAsync();

    /// <summary>
    /// Performs health check on a specific integration
    /// </summary>
    /// <param name="integrationId">Integration identifier</param>
    /// <returns>Health check result</returns>
    Task<Result<IntegrationHealthCheckDto>> CheckIntegrationHealthAsync(string integrationId);

    /// <summary>
    /// Gets configuration guide for a specific feature or integration
    /// </summary>
    /// <param name="id">Feature or integration identifier</param>
    /// <returns>Configuration guide</returns>
    Task<Result<ConfigurationGuideDto>> GetConfigurationGuideAsync(string id);

    /// <summary>
    /// Records feature usage for analytics
    /// </summary>
    /// <param name="featureId">Feature identifier</param>
    /// <param name="userId">User identifier</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> RecordFeatureUsageAsync(string featureId, Guid userId);

    /// <summary>
    /// Gets feature usage statistics
    /// </summary>
    /// <param name="featureId">Feature identifier</param>
    /// <param name="userId">Optional user identifier for user-specific stats</param>
    /// <returns>Feature usage statistics</returns>
    Task<Result<FeatureUsageDto>> GetFeatureUsageAsync(string featureId, Guid? userId = null);

    /// <summary>
    /// Refreshes all integration health checks
    /// </summary>
    /// <returns>Success result</returns>
    Task<Result<bool>> RefreshIntegrationHealthAsync();
} 