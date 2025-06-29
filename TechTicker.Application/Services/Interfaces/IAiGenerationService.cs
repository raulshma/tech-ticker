using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

public interface IAiGenerationService
{
    Task<Result<BrowserActionGenerationResponseDto>> GenerateBrowserActionsAsync(BrowserActionGenerationRequestDto request);
    Task<Result<bool>> IsAiConfigurationAvailableAsync();
}

public interface IAiProvider
{
    string ProviderName { get; }
    Task<Result<BrowserActionGenerationResponseDto>> GenerateBrowserActionsAsync(
        string instructions, 
        string? context, 
        AiConfigurationDto configuration);
    Task<Result<AiProviderModelsDto>> GetAvailableModelsAsync(string? baseUrl, string apiKey);
    Task<Result<bool>> TestConnectionAsync(AiConfigurationDto configuration);
} 