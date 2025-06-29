using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

public class AiConfigurationService : IAiConfigurationService
{
    private readonly IAiConfigurationRepository _repository;
    private readonly ILogger<AiConfigurationService> _logger;
    private readonly Dictionary<string, IAiProvider> _aiProviders;
    private const string EncryptionKey = "TechTicker-AI-Config-Key-2024-32Chars"; // In production, use a proper key management system

    public AiConfigurationService(
        IAiConfigurationRepository repository,
        ILogger<AiConfigurationService> logger,
        IEnumerable<IAiProvider> aiProviders)
    {
        _repository = repository;
        _logger = logger;
        _aiProviders = aiProviders.ToDictionary(p => p.ProviderName, p => p);
    }

    public async Task<Result<IEnumerable<AiConfigurationDto>>> GetAllConfigurationsAsync()
    {
        try
        {
            var configurations = await _repository.GetAllAsync();
            var dtos = configurations.Select(c => MapToDto(c)).ToList();
            return Result<IEnumerable<AiConfigurationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI configurations");
            return Result<IEnumerable<AiConfigurationDto>>.Failure("Failed to retrieve configurations");
        }
    }

    public async Task<Result<AiConfigurationDto>> GetConfigurationByIdAsync(Guid id)
    {
        try
        {
            var configuration = await _repository.GetByIdAsync(id);
            if (configuration == null)
            {
                return Result<AiConfigurationDto>.Failure("Configuration not found");
            }

            return Result<AiConfigurationDto>.Success(MapToDto(configuration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI configuration {Id}", id);
            return Result<AiConfigurationDto>.Failure("Failed to retrieve configuration");
        }
    }

    public async Task<Result<AiConfigurationDto>> GetDefaultConfigurationAsync()
    {
        try
        {
            var configuration = await _repository.GetDefaultConfigurationAsync();
            if (configuration == null)
            {
                return Result<AiConfigurationDto>.Failure("No default configuration found");
            }

            return Result<AiConfigurationDto>.Success(MapToDto(configuration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default AI configuration");
            return Result<AiConfigurationDto>.Failure("Failed to retrieve default configuration");
        }
    }

    public async Task<Result<AiConfigurationDto>> CreateConfigurationAsync(CreateAiConfigurationDto createDto, Guid createdBy)
    {
        try
        {
            // Check if configuration already exists
            var exists = await _repository.ExistsAsync(createDto.Provider, createDto.Model);
            if (exists)
            {
                return Result<AiConfigurationDto>.Failure("Configuration with this provider and model already exists");
            }

            var configuration = new AiConfiguration
            {
                AiConfigurationId = Guid.NewGuid(),
                Provider = createDto.Provider,
                Name = createDto.Name,
                Description = createDto.Description,
                OpenApiCompatibleUrl = createDto.OpenApiCompatibleUrl,
                ApiKey = EncryptApiKey(createDto.ApiKey),
                Model = createDto.Model,
                InputTokenLimit = createDto.InputTokenLimit,
                OutputTokenLimit = createDto.OutputTokenLimit,
                Capabilities = createDto.Capabilities != null ? JsonSerializer.Serialize(createDto.Capabilities) : null,
                SupportedInputTypes = createDto.SupportedInputTypes != null ? JsonSerializer.Serialize(createDto.SupportedInputTypes) : null,
                SupportedOutputTypes = createDto.SupportedOutputTypes != null ? JsonSerializer.Serialize(createDto.SupportedOutputTypes) : null,
                RateLimitRpm = createDto.RateLimitRpm,
                RateLimitTpm = createDto.RateLimitTpm,
                RateLimitRpd = createDto.RateLimitRpd,
                IsActive = createDto.IsActive,
                IsDefault = createDto.IsDefault,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            };

            var created = await _repository.CreateAsync(configuration);
            return Result<AiConfigurationDto>.Success(MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI configuration");
            return Result<AiConfigurationDto>.Failure("Failed to create configuration");
        }
    }

    public async Task<Result<AiConfigurationDto>> UpdateConfigurationAsync(Guid id, UpdateAiConfigurationDto updateDto, Guid updatedBy)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return Result<AiConfigurationDto>.Failure("Configuration not found");
            }

            // Update fields
            if (!string.IsNullOrEmpty(updateDto.Name))
                existing.Name = updateDto.Name;
            
            if (updateDto.Description != null)
                existing.Description = updateDto.Description;
            
            if (updateDto.OpenApiCompatibleUrl != null)
                existing.OpenApiCompatibleUrl = updateDto.OpenApiCompatibleUrl;
            
            if (!string.IsNullOrEmpty(updateDto.ApiKey))
                existing.ApiKey = EncryptApiKey(updateDto.ApiKey);
            
            if (!string.IsNullOrEmpty(updateDto.Model))
                existing.Model = updateDto.Model;
            
            if (updateDto.InputTokenLimit.HasValue)
                existing.InputTokenLimit = updateDto.InputTokenLimit.Value;
            
            if (updateDto.OutputTokenLimit.HasValue)
                existing.OutputTokenLimit = updateDto.OutputTokenLimit.Value;
            
            if (updateDto.Capabilities != null)
                existing.Capabilities = JsonSerializer.Serialize(updateDto.Capabilities);
            
            if (updateDto.SupportedInputTypes != null)
                existing.SupportedInputTypes = JsonSerializer.Serialize(updateDto.SupportedInputTypes);
            
            if (updateDto.SupportedOutputTypes != null)
                existing.SupportedOutputTypes = JsonSerializer.Serialize(updateDto.SupportedOutputTypes);
            
            if (updateDto.RateLimitRpm.HasValue)
                existing.RateLimitRpm = updateDto.RateLimitRpm.Value;
            
            if (updateDto.RateLimitTpm.HasValue)
                existing.RateLimitTpm = updateDto.RateLimitTpm.Value;
            
            if (updateDto.RateLimitRpd.HasValue)
                existing.RateLimitRpd = updateDto.RateLimitRpd.Value;
            
            if (updateDto.IsActive.HasValue)
                existing.IsActive = updateDto.IsActive.Value;
            
            if (updateDto.IsDefault.HasValue)
                existing.IsDefault = updateDto.IsDefault.Value;

            existing.UpdatedBy = updatedBy;

            var updated = await _repository.UpdateAsync(existing);
            return Result<AiConfigurationDto>.Success(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI configuration {Id}", id);
            return Result<AiConfigurationDto>.Failure("Failed to update configuration");
        }
    }

    public async Task<Result<bool>> DeleteConfigurationAsync(Guid id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id);
            return Result<bool>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting AI configuration {Id}", id);
            return Result<bool>.Failure("Failed to delete configuration");
        }
    }

    public async Task<Result<bool>> SetDefaultConfigurationAsync(Guid id)
    {
        try
        {
            var result = await _repository.SetDefaultAsync(id);
            return Result<bool>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default AI configuration {Id}", id);
            return Result<bool>.Failure("Failed to set default configuration");
        }
    }

    public async Task<Result<IEnumerable<AiConfigurationDto>>> GetActiveConfigurationsAsync()
    {
        try
        {
            var configurations = await _repository.GetActiveConfigurationsAsync();
            var dtos = configurations.Select(c => MapToDto(c)).ToList();
            return Result<IEnumerable<AiConfigurationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active AI configurations");
            return Result<IEnumerable<AiConfigurationDto>>.Failure("Failed to retrieve active configurations");
        }
    }

    public async Task<Result<AiProviderModelsDto>> GetAvailableModelsAsync(string provider, string? baseUrl, string apiKey)
    {
        try
        {
            if (!_aiProviders.TryGetValue(provider, out var aiProvider))
            {
                return Result<AiProviderModelsDto>.Failure($"Provider '{provider}' not supported");
            }

            return await aiProvider.GetAvailableModelsAsync(baseUrl, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models for provider {Provider}", provider);
            return Result<AiProviderModelsDto>.Failure("Failed to get available models");
        }
    }

    public async Task<Result<bool>> TestConfigurationAsync(Guid id)
    {
        try
        {
            var config = await _repository.GetByIdAsync(id);
            if (config == null)
            {
                return Result<bool>.Failure("Configuration not found");
            }

            if (!_aiProviders.TryGetValue(config.Provider, out var aiProvider))
            {
                return Result<bool>.Failure($"Provider '{config.Provider}' not supported");
            }

            var configDto = MapToDto(config);
            // Decrypt the API key for testing
            var decryptedConfig = configDto;
            decryptedConfig.DecryptedApiKey = DecryptApiKey(config.ApiKey);
            return await aiProvider.TestConnectionAsync(decryptedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing AI configuration {Id}", id);
            return Result<bool>.Failure("Failed to test configuration");
        }
    }

    private AiConfigurationDto MapToDto(AiConfiguration entity, bool includeApiKey = false)
    {
        var dto = new AiConfigurationDto
        {
            AiConfigurationId = entity.AiConfigurationId,
            Provider = entity.Provider,
            Name = entity.Name,
            Description = entity.Description,
            OpenApiCompatibleUrl = entity.OpenApiCompatibleUrl,
            Model = entity.Model,
            InputTokenLimit = entity.InputTokenLimit,
            OutputTokenLimit = entity.OutputTokenLimit,
            Capabilities = !string.IsNullOrEmpty(entity.Capabilities) 
                ? JsonSerializer.Deserialize<List<string>>(entity.Capabilities) 
                : null,
            SupportedInputTypes = !string.IsNullOrEmpty(entity.SupportedInputTypes) 
                ? JsonSerializer.Deserialize<List<string>>(entity.SupportedInputTypes) 
                : null,
            SupportedOutputTypes = !string.IsNullOrEmpty(entity.SupportedOutputTypes) 
                ? JsonSerializer.Deserialize<List<string>>(entity.SupportedOutputTypes) 
                : null,
            RateLimitRpm = entity.RateLimitRpm,
            RateLimitTpm = entity.RateLimitTpm,
            RateLimitRpd = entity.RateLimitRpd,
            IsActive = entity.IsActive,
            IsDefault = entity.IsDefault,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };

        if (includeApiKey)
        {
            dto.DecryptedApiKey = DecryptApiKey(entity.ApiKey);
        }

        return dto;
    }

    private string EncryptApiKey(string apiKey)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16]; // Using zero IV for simplicity - in production, use proper IV

            using var encryptor = aes.CreateEncryptor();
            var apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = encryptor.TransformFinalBlock(apiKeyBytes, 0, apiKeyBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting API key");
            throw;
        }
    }

    private string DecryptApiKey(string encryptedApiKey)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.IV = new byte[16]; // Using zero IV for simplicity - in production, use proper IV

            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedApiKey);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting API key");
            throw;
        }
    }

    public async Task<Result<AiConfigurationDto>> GetConfigurationForProviderAsync(Guid id)
    {
        try
        {
            var configuration = await _repository.GetByIdAsync(id);
            if (configuration == null)
            {
                return Result<AiConfigurationDto>.Failure("Configuration not found");
            }

            var dto = MapToDto(configuration, includeApiKey: true);
            return Result<AiConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI configuration {Id} for provider", id);
            return Result<AiConfigurationDto>.Failure("Failed to retrieve configuration");
        }
    }

    public async Task<Result<AiConfigurationDto>> GetDefaultConfigurationForProviderAsync()
    {
        try
        {
            var configuration = await _repository.GetDefaultConfigurationAsync();
            if (configuration == null)
            {
                return Result<AiConfigurationDto>.Failure("No default configuration found");
            }

            var dto = MapToDto(configuration, includeApiKey: true);
            return Result<AiConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default AI configuration for provider");
            return Result<AiConfigurationDto>.Failure("Failed to retrieve default configuration");
        }
    }
} 