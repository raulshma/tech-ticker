using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for AI Configuration operations
/// </summary>
[Route("api/ai-configurations")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AiConfigurationController : BaseApiController
{
    private readonly IAiConfigurationService _aiConfigurationService;
    private readonly IAiGenerationService _aiGenerationService;

    public AiConfigurationController(
        IAiConfigurationService aiConfigurationService,
        IAiGenerationService aiGenerationService)
    {
        _aiConfigurationService = aiConfigurationService;
        _aiGenerationService = aiGenerationService;
    }

    /// <summary>
    /// Get all AI configurations
    /// </summary>
    /// <returns>List of all AI configurations</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AiConfigurationDto>>>> GetAllConfigurations()
    {
        var result = await _aiConfigurationService.GetAllConfigurationsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get active AI configurations
    /// </summary>
    /// <returns>List of active AI configurations</returns>
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AiConfigurationDto>>>> GetActiveConfigurations()
    {
        var result = await _aiConfigurationService.GetActiveConfigurationsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get AI configuration by ID
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>AI configuration</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AiConfigurationDto>>> GetConfigurationById(Guid id)
    {
        var result = await _aiConfigurationService.GetConfigurationByIdAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Get default AI configuration
    /// </summary>
    /// <returns>Default AI configuration</returns>
    [HttpGet("default")]
    [Authorize(Roles = "User,Admin")] // Allow users to check if AI is available
    public async Task<ActionResult<ApiResponse<AiConfigurationDto>>> GetDefaultConfiguration()
    {
        var result = await _aiConfigurationService.GetDefaultConfigurationAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new AI configuration
    /// </summary>
    /// <param name="createDto">AI configuration creation data</param>
    /// <returns>Created AI configuration</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AiConfigurationDto>>> CreateConfiguration([FromBody] CreateAiConfigurationDto createDto)
    {
        var userId = GetCurrentUserId();
        var result = await _aiConfigurationService.CreateConfigurationAsync(createDto, userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing AI configuration
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <param name="updateDto">AI configuration update data</param>
    /// <returns>Updated AI configuration</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AiConfigurationDto>>> UpdateConfiguration(Guid id, [FromBody] UpdateAiConfigurationDto updateDto)
    {
        var userId = GetCurrentUserId();
        var result = await _aiConfigurationService.UpdateConfigurationAsync(id, updateDto, userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete an AI configuration
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteConfiguration(Guid id)
    {
        var result = await _aiConfigurationService.DeleteConfigurationAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Set an AI configuration as default
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult<ApiResponse<bool>>> SetDefaultConfiguration(Guid id)
    {
        var result = await _aiConfigurationService.SetDefaultConfigurationAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Test an AI configuration connection
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Test result</returns>
    [HttpPost("{id:guid}/test")]
    public async Task<ActionResult<ApiResponse<bool>>> TestConfiguration(Guid id)
    {
        var result = await _aiConfigurationService.TestConfigurationAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Get available models for a provider
    /// </summary>
    /// <param name="provider">AI provider name</param>
    /// <param name="baseUrl">Optional base URL</param>
    /// <param name="apiKey">API key</param>
    /// <returns>List of available models</returns>
    [HttpPost("models/{provider}")]
    public async Task<ActionResult<ApiResponse<AiProviderModelsDto>>> GetAvailableModels(
        string provider, 
        [FromBody] ProviderModelsRequestDto request)
    {
        var result = await _aiConfigurationService.GetAvailableModelsAsync(provider, request.BaseUrl, request.ApiKey);
        return HandleResult(result);
    }

    /// <summary>
    /// Generate browser actions using AI
    /// </summary>
    /// <param name="request">Generation request</param>
    /// <returns>Generated browser actions</returns>
    [HttpPost("generate-browser-actions")]
    [Authorize(Roles = "User,Admin")] // Allow users to generate actions
    public async Task<ActionResult<ApiResponse<BrowserActionGenerationResponseDto>>> GenerateBrowserActions([FromBody] BrowserActionGenerationRequestDto request)
    {
        var result = await _aiGenerationService.GenerateBrowserActionsAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Check if AI generation is available
    /// </summary>
    /// <returns>Availability status</returns>
    [HttpGet("availability")]
    [Authorize(Roles = "User,Admin")] // Allow users to check availability
    public async Task<ActionResult<ApiResponse<bool>>> CheckAiAvailability()
    {
        var result = await _aiGenerationService.IsAiConfigurationAvailableAsync();
        return HandleResult(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }
}

/// <summary>
/// Request DTO for getting provider models
/// </summary>
public class ProviderModelsRequestDto
{
    public string? BaseUrl { get; set; }
    public string ApiKey { get; set; } = null!;
} 