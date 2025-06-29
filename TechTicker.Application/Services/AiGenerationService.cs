using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

public class AiGenerationService : IAiGenerationService
{
    private readonly IAiConfigurationService _configurationService;
    private readonly Dictionary<string, IAiProvider> _aiProviders;
    private readonly ILogger<AiGenerationService> _logger;

    public AiGenerationService(
        IAiConfigurationService configurationService,
        IEnumerable<IAiProvider> aiProviders,
        ILogger<AiGenerationService> logger)
    {
        _configurationService = configurationService;
        _aiProviders = aiProviders.ToDictionary(p => p.ProviderName, p => p);
        _logger = logger;
    }

    public async Task<Result<BrowserActionGenerationResponseDto>> GenerateBrowserActionsAsync(BrowserActionGenerationRequestDto request)
    {
        try
        {
            // Get the AI configuration to use
            AiConfigurationDto configuration;
            
            if (request.AiConfigurationId.HasValue)
            {
                var configResult = await _configurationService.GetConfigurationForProviderAsync(request.AiConfigurationId.Value);
                if (!configResult.IsSuccess)
                {
                    return Result<BrowserActionGenerationResponseDto>.Failure($"Configuration not found: {configResult.ErrorMessage}");
                }
                configuration = configResult.Data!;
            }
            else
            {
                // Use default configuration
                var defaultConfigResult = await _configurationService.GetDefaultConfigurationForProviderAsync();
                if (!defaultConfigResult.IsSuccess)
                {
                    return Result<BrowserActionGenerationResponseDto>.Failure("No default AI configuration found. Please configure an AI provider first.");
                }
                configuration = defaultConfigResult.Data!;
            }

            // Check if configuration is active
            if (!configuration.IsActive)
            {
                return Result<BrowserActionGenerationResponseDto>.Failure("The selected AI configuration is not active");
            }

            // Get the appropriate AI provider
            if (!_aiProviders.TryGetValue(configuration.Provider, out var aiProvider))
            {
                return Result<BrowserActionGenerationResponseDto>.Failure($"AI provider '{configuration.Provider}' is not supported");
            }

            // Generate the browser actions
            _logger.LogInformation("Generating browser actions using {Provider} with model {Model}", 
                configuration.Provider, configuration.Model);

            var result = await aiProvider.GenerateBrowserActionsAsync(
                request.Instructions,
                request.Context,
                configuration);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully generated {ActionCount} browser actions using {TokenCount} tokens",
                    result.Data!.Actions.Count, result.Data.TokensUsed);
            }
            else
            {
                _logger.LogWarning("Failed to generate browser actions: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating browser actions");
            return Result<BrowserActionGenerationResponseDto>.Failure($"Generation failed: {ex.Message}");
        }
    }

    public async Task<Result<GenericAiResponseDto>> GenerateGenericResponseAsync(GenericAiRequestDto request)
    {
        try
        {
            // Get the AI configuration to use
            AiConfigurationDto configuration;
            
            if (request.AiConfigurationId.HasValue)
            {
                var configResult = await _configurationService.GetConfigurationForProviderAsync(request.AiConfigurationId.Value);
                if (!configResult.IsSuccess)
                {
                    return Result<GenericAiResponseDto>.Failure($"Configuration not found: {configResult.ErrorMessage}");
                }
                configuration = configResult.Data!;
            }
            else
            {
                // Use default configuration
                var defaultConfigResult = await _configurationService.GetDefaultConfigurationForProviderAsync();
                if (!defaultConfigResult.IsSuccess)
                {
                    return Result<GenericAiResponseDto>.Failure("No default AI configuration found. Please configure an AI provider first.");
                }
                configuration = defaultConfigResult.Data!;
            }

            // Check if configuration is active
            if (!configuration.IsActive)
            {
                return Result<GenericAiResponseDto>.Failure("The selected AI configuration is not active");
            }

            // Get the appropriate AI provider
            if (!_aiProviders.TryGetValue(configuration.Provider, out var aiProvider))
            {
                return Result<GenericAiResponseDto>.Failure($"AI provider '{configuration.Provider}' is not supported");
            }

            // Generate the generic response
            _logger.LogInformation("Generating generic AI response using {Provider} with model {Model}", 
                configuration.Provider, configuration.Model);

            var result = await aiProvider.GenerateGenericResponseAsync(
                request.InputText,
                request.SystemPrompt,
                request.Context,
                request.JsonSchema,
                request.Temperature,
                request.MaxTokens,
                configuration);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully generated generic AI response using {TokenCount} tokens",
                    result.Data!.TokensUsed);
            }
            else
            {
                _logger.LogWarning("Failed to generate generic AI response: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating generic AI response");
            return Result<GenericAiResponseDto>.Failure($"Generation failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsAiConfigurationAvailableAsync()
    {
        try
        {
            var defaultConfigResult = await _configurationService.GetDefaultConfigurationAsync();
            if (!defaultConfigResult.IsSuccess)
            {
                return Result<bool>.Success(false);
            }

            var configuration = defaultConfigResult.Data!;
            return Result<bool>.Success(configuration.IsActive && _aiProviders.ContainsKey(configuration.Provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI configuration availability");
            return Result<bool>.Failure($"Failed to check availability: {ex.Message}");
        }
    }
} 