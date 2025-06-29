using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

public interface IAiConfigurationService
{
    Task<Result<IEnumerable<AiConfigurationDto>>> GetAllConfigurationsAsync();
    Task<Result<AiConfigurationDto>> GetConfigurationByIdAsync(Guid id);
    Task<Result<AiConfigurationDto>> GetDefaultConfigurationAsync();
    Task<Result<AiConfigurationDto>> CreateConfigurationAsync(CreateAiConfigurationDto createDto, Guid createdBy);
    Task<Result<AiConfigurationDto>> UpdateConfigurationAsync(Guid id, UpdateAiConfigurationDto updateDto, Guid updatedBy);
    Task<Result<bool>> DeleteConfigurationAsync(Guid id);
    Task<Result<bool>> SetDefaultConfigurationAsync(Guid id);
    Task<Result<IEnumerable<AiConfigurationDto>>> GetActiveConfigurationsAsync();
    Task<Result<AiProviderModelsDto>> GetAvailableModelsAsync(string provider, string? baseUrl, string apiKey);
    Task<Result<bool>> TestConfigurationAsync(Guid id);
    Task<Result<AiConfigurationDto>> GetConfigurationForProviderAsync(Guid id);
    Task<Result<AiConfigurationDto>> GetDefaultConfigurationForProviderAsync();
} 