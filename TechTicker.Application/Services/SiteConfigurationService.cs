using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing site configurations
/// </summary>
public class SiteConfigurationService : ISiteConfigurationService
{
    private readonly ISiteConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<SiteConfigurationService> _logger;

    public SiteConfigurationService(
        ISiteConfigurationRepository repository,
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<SiteConfigurationService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<SiteConfigurationDto>> CreateConfigurationAsync(SaveSiteConfigurationRequest request, Guid userId)
    {
        try
        {
            // Check if domain already exists
            if (await _repository.DomainExistsAsync(request.Domain))
            {
                return Result<SiteConfigurationDto>.Failure($"Configuration for domain '{request.Domain}' already exists");
            }

            var entity = new SiteConfiguration
            {
                Id = Guid.NewGuid(),
                Domain = request.Domain.ToLowerInvariant(),
                SiteName = request.SiteName,
                IsActive = request.IsActive,
                Notes = request.Notes,
                TestHtml = request.TestHtml,
                ProductNameSelectorsArray = request.Selectors.ProductNameSelectors,
                PriceSelectorsArray = request.Selectors.PriceSelectors,
                ImageSelectorsArray = request.Selectors.ImageSelectors,
                DescriptionSelectorsArray = request.Selectors.DescriptionSelectors,
                ManufacturerSelectorsArray = request.Selectors.ManufacturerSelectors,
                ModelNumberSelectorsArray = request.Selectors.ModelNumberSelectors,
                SpecificationSelectorsArray = request.Selectors.SpecificationSelectors,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                UpdatedByUserId = userId
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = MapSiteConfigurationToDto(entity);
            _logger.LogInformation("Created site configuration for domain: {Domain}", request.Domain);

            return Result<SiteConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site configuration for domain: {Domain}", request.Domain);
            return Result<SiteConfigurationDto>.Failure(ex);
        }
    }

    public async Task<Result<SiteConfigurationDto>> UpdateConfigurationAsync(Guid id, SaveSiteConfigurationRequest request, Guid userId)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<SiteConfigurationDto>.Failure("Site configuration not found");
            }

            // Check if domain already exists (excluding current entity)
            if (entity.Domain != request.Domain.ToLowerInvariant() && 
                await _repository.DomainExistsAsync(request.Domain, id))
            {
                return Result<SiteConfigurationDto>.Failure($"Configuration for domain '{request.Domain}' already exists");
            }

            entity.Domain = request.Domain.ToLowerInvariant();
            entity.SiteName = request.SiteName;
            entity.IsActive = request.IsActive;
            entity.Notes = request.Notes;
            entity.TestHtml = request.TestHtml;
            entity.ProductNameSelectorsArray = request.Selectors.ProductNameSelectors;
            entity.PriceSelectorsArray = request.Selectors.PriceSelectors;
            entity.ImageSelectorsArray = request.Selectors.ImageSelectors;
            entity.DescriptionSelectorsArray = request.Selectors.DescriptionSelectors;
            entity.ManufacturerSelectorsArray = request.Selectors.ManufacturerSelectors;
            entity.ModelNumberSelectorsArray = request.Selectors.ModelNumberSelectors;
            entity.SpecificationSelectorsArray = request.Selectors.SpecificationSelectors;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = userId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = MapSiteConfigurationToDto(entity);
            _logger.LogInformation("Updated site configuration for domain: {Domain}", request.Domain);

            return Result<SiteConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating site configuration: {Id}", id);
            return Result<SiteConfigurationDto>.Failure(ex);
        }
    }

    public async Task<Result<SiteConfigurationDto>> GetConfigurationAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<SiteConfigurationDto>.Failure("Site configuration not found");
            }

            var dto = MapSiteConfigurationToDto(entity);
            return Result<SiteConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting site configuration: {Id}", id);
            return Result<SiteConfigurationDto>.Failure(ex);
        }
    }

    public async Task<Result<SiteConfigurationDto>> GetConfigurationByDomainAsync(string domain)
    {
        try
        {
            var entity = await _repository.GetByDomainAsync(domain);
            if (entity == null)
            {
                return Result<SiteConfigurationDto>.Failure($"No configuration found for domain: {domain}");
            }

            var dto = MapSiteConfigurationToDto(entity);
            return Result<SiteConfigurationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting site configuration by domain: {Domain}", domain);
            return Result<SiteConfigurationDto>.Failure(ex);
        }
    }

    public async Task<Result<PagedResponse<SiteConfigurationDto>>> GetConfigurationsAsync(SiteConfigurationFilterRequest filter)
    {
        try
        {
            var result = await _repository.GetConfigurationsAsync(
                filter.Domain,
                filter.SiteName,
                filter.IsActive,
                filter.Page,
                filter.PageSize,
                filter.SortBy,
                filter.SortDescending);

            var dtos = result.Items.Select(MapSiteConfigurationToDto).ToList();
            var pagedResponse = PagedResponse<SiteConfigurationDto>.SuccessResult(
                dtos, filter.Page, filter.PageSize, result.TotalCount);

            return Result<PagedResponse<SiteConfigurationDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting site configurations");
            return Result<PagedResponse<SiteConfigurationDto>>.Failure(ex);
        }
    }

    public async Task<Result<List<SiteConfigurationDto>>> GetActiveConfigurationsAsync()
    {
        try
        {
            var entities = await _repository.GetActiveConfigurationsAsync();
            var dtos = entities.Select(MapSiteConfigurationToDto).ToList();
            return Result<List<SiteConfigurationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active site configurations");
            return Result<List<SiteConfigurationDto>>.Failure(ex);
        }
    }

    public async Task<Result> DeleteConfigurationAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result.Failure("Site configuration not found");
            }

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted site configuration: {Id} for domain: {Domain}", id, entity.Domain);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting site configuration: {Id}", id);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> UpdateTestResultAsync(Guid id, SelectorTestResult testResult)
    {
        try
        {
            var testResultJson = System.Text.Json.JsonSerializer.Serialize(testResult);
            await _repository.UpdateTestResultAsync(id, testResultJson, (decimal)testResult.OverallScore);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated test result for site configuration: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test result for site configuration: {Id}", id);
            return Result.Failure(ex);
        }
    }

    public async Task<Result<List<SiteConfigurationDto>>> GetConfigurationsNeedingTestAsync(TimeSpan maxAge)
    {
        try
        {
            var entities = await _repository.GetConfigurationsNeedingTestAsync(maxAge);
            var dtos = entities.Select(MapSiteConfigurationToDto).ToList();
            return Result<List<SiteConfigurationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations needing test");
            return Result<List<SiteConfigurationDto>>.Failure(ex);
        }
    }

    private static SiteConfigurationDto MapSiteConfigurationToDto(SiteConfiguration entity)
    {
        return new SiteConfigurationDto
        {
            Id = entity.Id,
            Domain = entity.Domain,
            SiteName = entity.SiteName,
            IsActive = entity.IsActive,
            Notes = entity.Notes,
            TestHtml = entity.TestHtml,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedByUserId = entity.CreatedByUserId?.ToString(),
            UpdatedByUserId = entity.UpdatedByUserId?.ToString(),
            Selectors = new SelectorSet
            {
                Domain = entity.Domain,
                ProductNameSelectors = entity.ProductNameSelectorsArray,
                PriceSelectors = entity.PriceSelectorsArray,
                ImageSelectors = entity.ImageSelectorsArray,
                DescriptionSelectors = entity.DescriptionSelectorsArray,
                ManufacturerSelectors = entity.ManufacturerSelectorsArray,
                ModelNumberSelectors = entity.ModelNumberSelectorsArray,
                SpecificationSelectors = entity.SpecificationSelectorsArray,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            }
        };
    }
}
