using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for ScraperSiteConfiguration operations
/// </summary>
public class ScraperSiteConfigurationService : IScraperSiteConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<ScraperSiteConfigurationService> _logger;

    public ScraperSiteConfigurationService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<ScraperSiteConfigurationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<ScraperSiteConfigurationDto>> CreateConfigurationAsync(CreateScraperSiteConfigurationDto createDto)
    {
        try
        {
            // Check if domain already exists
            if (await _unitOfWork.ScraperSiteConfigurations.DomainExistsAsync(createDto.SiteDomain))
            {
                return Result<ScraperSiteConfigurationDto>.Failure("A configuration for this domain already exists.", "DOMAIN_EXISTS");
            }

            var config = _mappingService.MapToEntity(createDto);
            await _unitOfWork.ScraperSiteConfigurations.AddAsync(config);
            await _unitOfWork.SaveChangesAsync();

            var configDto = _mappingService.MapToDto(config);
            _logger.LogInformation("Created scraper site configuration {ConfigId} for domain {Domain}", config.SiteConfigId, config.SiteDomain);

            return Result<ScraperSiteConfigurationDto>.Success(configDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scraper site configuration for domain {Domain}", createDto.SiteDomain);
            return Result<ScraperSiteConfigurationDto>.Failure("An error occurred while creating the configuration.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ScraperSiteConfigurationDto>> GetConfigurationByIdAsync(Guid configId)
    {
        try
        {
            var config = await _unitOfWork.ScraperSiteConfigurations.GetByIdAsync(configId);
            if (config == null)
            {
                return Result<ScraperSiteConfigurationDto>.Failure("Configuration not found.", "RESOURCE_NOT_FOUND");
            }

            var configDto = _mappingService.MapToDto(config);
            return Result<ScraperSiteConfigurationDto>.Success(configDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scraper site configuration {ConfigId}", configId);
            return Result<ScraperSiteConfigurationDto>.Failure("An error occurred while retrieving the configuration.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ScraperSiteConfigurationDto>> GetConfigurationByDomainAsync(string domain)
    {
        try
        {
            var config = await _unitOfWork.ScraperSiteConfigurations.GetByDomainAsync(domain);
            if (config == null)
            {
                return Result<ScraperSiteConfigurationDto>.Failure("Configuration not found.", "RESOURCE_NOT_FOUND");
            }

            var configDto = _mappingService.MapToDto(config);
            return Result<ScraperSiteConfigurationDto>.Success(configDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scraper site configuration for domain {Domain}", domain);
            return Result<ScraperSiteConfigurationDto>.Failure("An error occurred while retrieving the configuration.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ScraperSiteConfigurationDto>> UpdateConfigurationAsync(Guid configId, UpdateScraperSiteConfigurationDto updateDto)
    {
        try
        {
            var config = await _unitOfWork.ScraperSiteConfigurations.GetByIdAsync(configId);
            if (config == null)
            {
                return Result<ScraperSiteConfigurationDto>.Failure("Configuration not found.", "RESOURCE_NOT_FOUND");
            }

            // Check if new domain conflicts with existing configurations
            if (!string.IsNullOrWhiteSpace(updateDto.SiteDomain) && updateDto.SiteDomain != config.SiteDomain)
            {
                if (await _unitOfWork.ScraperSiteConfigurations.DomainExistsAsync(updateDto.SiteDomain, configId))
                {
                    return Result<ScraperSiteConfigurationDto>.Failure("A configuration for this domain already exists.", "DOMAIN_EXISTS");
                }
            }

            _mappingService.MapToEntity(updateDto, config);
            _unitOfWork.ScraperSiteConfigurations.Update(config);
            await _unitOfWork.SaveChangesAsync();

            var configDto = _mappingService.MapToDto(config);
            _logger.LogInformation("Updated scraper site configuration {ConfigId}", configId);

            return Result<ScraperSiteConfigurationDto>.Success(configDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scraper site configuration {ConfigId}", configId);
            return Result<ScraperSiteConfigurationDto>.Failure("An error occurred while updating the configuration.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteConfigurationAsync(Guid configId)
    {
        try
        {
            var config = await _unitOfWork.ScraperSiteConfigurations.GetByIdAsync(configId);
            if (config == null)
            {
                return Result.Failure("Configuration not found.", "RESOURCE_NOT_FOUND");
            }

            // Check if configuration is in use by mappings
            var mappingsUsingConfig = await _unitOfWork.ProductSellerMappings.FindAsync(m => m.SiteConfigId == configId);
            if (mappingsUsingConfig.Any())
            {
                return Result.Failure("Cannot delete configuration that is in use by product seller mappings.", "CONFIG_IN_USE");
            }

            _unitOfWork.ScraperSiteConfigurations.Remove(config);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted scraper site configuration {ConfigId}", configId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scraper site configuration {ConfigId}", configId);
            return Result.Failure("An error occurred while deleting the configuration.", "INTERNAL_ERROR");
        }
    }
}
