using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;
using System.Security.Claims;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// Controller for managing integrations and features overview
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IntegrationsAndFeaturesController : BaseApiController
{
    private readonly IIntegrationsAndFeaturesService _integrationsAndFeaturesService;

    public IntegrationsAndFeaturesController(IIntegrationsAndFeaturesService integrationsAndFeaturesService)
    {
        _integrationsAndFeaturesService = integrationsAndFeaturesService;
    }

    /// <summary>
    /// Gets the complete overview of all features and integrations
    /// </summary>
    /// <returns>Complete integrations and features overview</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IntegrationsAndFeaturesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IntegrationsAndFeaturesDto>>> GetIntegrationsAndFeatures()
    {
        var userId = GetCurrentUserId();
        var result = await _integrationsAndFeaturesService.GetIntegrationsAndFeaturesAsync(userId);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<IntegrationsAndFeaturesDto>.SuccessResult(result.Data, "System overview retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets all available features with their current status
    /// </summary>
    /// <returns>List of features with status</returns>
    [HttpGet("features")]
    [ProducesResponseType(typeof(ApiResponse<List<FeatureDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<FeatureDto>>>> GetFeatures()
    {
        var userId = GetCurrentUserId();
        var result = await _integrationsAndFeaturesService.GetFeaturesAsync(userId);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<List<FeatureDto>>.SuccessResult(result.Data, "Features retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets all integrations with their current status
    /// </summary>
    /// <returns>List of integrations with status</returns>
    [HttpGet("integrations")]
    [ProducesResponseType(typeof(ApiResponse<List<IntegrationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<IntegrationDto>>>> GetIntegrations()
    {
        var result = await _integrationsAndFeaturesService.GetIntegrationsAsync();
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<List<IntegrationDto>>.SuccessResult(result.Data, "Integrations retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets system health overview
    /// </summary>
    /// <returns>System health information</returns>
    [HttpGet("system-health")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SystemHealthDto>>> GetSystemHealth()
    {
        var result = await _integrationsAndFeaturesService.GetSystemHealthAsync();
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<SystemHealthDto>.SuccessResult(result.Data, "System health retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Performs health check on a specific integration
    /// </summary>
    /// <param name="integrationId">Integration identifier</param>
    /// <returns>Health check result</returns>
    [HttpPost("integrations/{integrationId}/health-check")]
    [ProducesResponseType(typeof(ApiResponse<IntegrationHealthCheckDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IntegrationHealthCheckDto>>> CheckIntegrationHealth(string integrationId)
    {
        if (string.IsNullOrWhiteSpace(integrationId))
        {
            var errorResponse = ApiResponse<object>.FailureResult("Integration ID is required");
            return new BadRequestObjectResult(errorResponse);
        }

        var result = await _integrationsAndFeaturesService.CheckIntegrationHealthAsync(integrationId);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<IntegrationHealthCheckDto>.SuccessResult(result.Data, "Health check completed");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets configuration guide for a specific feature or integration
    /// </summary>
    /// <param name="id">Feature or integration identifier</param>
    /// <returns>Configuration guide</returns>
    [HttpGet("configuration-guide/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationGuideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ConfigurationGuideDto>>> GetConfigurationGuide(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            var errorResponse = ApiResponse<object>.FailureResult("ID is required");
            return new BadRequestObjectResult(errorResponse);
        }

        var result = await _integrationsAndFeaturesService.GetConfigurationGuideAsync(id);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new NotFoundObjectResult(errorResponse);
        }

        var response = ApiResponse<ConfigurationGuideDto>.SuccessResult(result.Data, "Configuration guide retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Records feature usage for analytics
    /// </summary>
    /// <param name="featureId">Feature identifier</param>
    /// <returns>Success result</returns>
    [HttpPost("features/{featureId}/usage")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> RecordFeatureUsage(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            var errorResponse = ApiResponse<object>.FailureResult("Feature ID is required");
            return new BadRequestObjectResult(errorResponse);
        }

        var userId = GetCurrentUserId();
        var result = await _integrationsAndFeaturesService.RecordFeatureUsageAsync(featureId, userId);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<bool>.SuccessResult(result.Data, "Feature usage recorded");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets feature usage statistics
    /// </summary>
    /// <param name="featureId">Feature identifier</param>
    /// <returns>Feature usage statistics</returns>
    [HttpGet("features/{featureId}/usage")]
    [ProducesResponseType(typeof(ApiResponse<FeatureUsageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<FeatureUsageDto>>> GetFeatureUsage(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            var errorResponse = ApiResponse<object>.FailureResult("Feature ID is required");
            return new BadRequestObjectResult(errorResponse);
        }

        var userId = GetCurrentUserId();
        var result = await _integrationsAndFeaturesService.GetFeatureUsageAsync(featureId, userId);
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<FeatureUsageDto>.SuccessResult(result.Data, "Feature usage retrieved successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Refreshes all integration health checks
    /// </summary>
    /// <returns>Success result</returns>
    [HttpPost("integrations/refresh-health")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> RefreshIntegrationHealth()
    {
        var result = await _integrationsAndFeaturesService.RefreshIntegrationHealthAsync();
        
        if (!result.IsSuccess)
        {
            var errorResponse = ApiResponse<object>.FailureResult(result.ErrorMessage);
            return new BadRequestObjectResult(errorResponse);
        }

        var response = ApiResponse<bool>.SuccessResult(result.Data, "Integration health refreshed successfully");
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Gets the current user's ID from the JWT token
    /// </summary>
    /// <returns>Current user's ID</returns>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
} 