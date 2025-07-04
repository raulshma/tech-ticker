using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for ProductSellerMapping operations
/// </summary>
public class ProductSellerMappingService : IProductSellerMappingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<ProductSellerMappingService> _logger;

    public ProductSellerMappingService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<ProductSellerMappingService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<ProductSellerMappingDto>> CreateMappingAsync(CreateProductSellerMappingDto createDto)
    {
        try
        {
            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == createDto.CanonicalProductId);
            if (!productExists)
            {
                return Result<ProductSellerMappingDto>.Failure("Product not found.", "PRODUCT_NOT_FOUND");
            }

            // Validate site configuration exists if provided
            if (createDto.SiteConfigId.HasValue)
            {
                var configExists = await _unitOfWork.ScraperSiteConfigurations.ExistsAsync(s => s.SiteConfigId == createDto.SiteConfigId.Value);
                if (!configExists)
                {
                    return Result<ProductSellerMappingDto>.Failure("Site configuration not found.", "SITE_CONFIG_NOT_FOUND");
                }
            }

            // Check for duplicate mapping
            var existingMapping = await _unitOfWork.ProductSellerMappings.FirstOrDefaultAsync(m =>
                m.CanonicalProductId == createDto.CanonicalProductId &&
                m.SellerName == createDto.SellerName &&
                m.ExactProductUrl == createDto.ExactProductUrl);

            if (existingMapping != null)
            {
                return Result<ProductSellerMappingDto>.Failure("A mapping with this product, seller, and URL already exists.", "MAPPING_EXISTS");
            }

            var mapping = _mappingService.MapToEntity(createDto);
            await _unitOfWork.ProductSellerMappings.AddAsync(mapping);
            await _unitOfWork.SaveChangesAsync();

            // Reload with related data
            var mappingWithRelated = await _unitOfWork.ProductSellerMappings.GetByIdWithConfigurationAsync(mapping.MappingId);
            var mappingDto = _mappingService.MapToDto(mappingWithRelated!);

            _logger.LogInformation("Created product seller mapping {MappingId} for product {ProductId}", mapping.MappingId, createDto.CanonicalProductId);
            return Result<ProductSellerMappingDto>.Success(mappingDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product seller mapping for product {ProductId}", createDto.CanonicalProductId);
            return Result<ProductSellerMappingDto>.Failure("An error occurred while creating the mapping.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<ProductSellerMappingDto>>> GetMappingsByProductIdAsync(Guid productId, bool? isActiveForScraping = null)
    {
        try
        {
            var mappings = await _unitOfWork.ProductSellerMappings.GetByProductIdAsync(productId, isActiveForScraping);
            var mappingDtos = mappings.Select(_mappingService.MapToDto);

            return Result<IEnumerable<ProductSellerMappingDto>>.Success(mappingDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mappings for product {ProductId}", productId);
            return Result<IEnumerable<ProductSellerMappingDto>>.Failure("An error occurred while retrieving mappings.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<ProductSellerMappingDto>>> GetActiveMappingsAsync()
    {
        try
        {
            var mappings = await _unitOfWork.ProductSellerMappings.GetActiveMappingsAsync();
            var mappingDtos = mappings.Select(_mappingService.MapToDto);

            return Result<IEnumerable<ProductSellerMappingDto>>.Success(mappingDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active mappings");
            return Result<IEnumerable<ProductSellerMappingDto>>.Failure("An error occurred while retrieving active mappings.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ProductSellerMappingDto>> UpdateMappingAsync(Guid mappingId, UpdateProductSellerMappingDto updateDto)
    {
        try
        {
            var mapping = await _unitOfWork.ProductSellerMappings.GetByIdAsync(mappingId);
            if (mapping == null)
            {
                return Result<ProductSellerMappingDto>.Failure("Mapping not found.", "RESOURCE_NOT_FOUND");
            }

            // Validate site configuration exists if being updated
            if (updateDto.SiteConfigId.HasValue)
            {
                var configExists = await _unitOfWork.ScraperSiteConfigurations.ExistsAsync(s => s.SiteConfigId == updateDto.SiteConfigId.Value);
                if (!configExists)
                {
                    return Result<ProductSellerMappingDto>.Failure("Site configuration not found.", "SITE_CONFIG_NOT_FOUND");
                }
            }

            _mappingService.MapToEntity(updateDto, mapping);
            _unitOfWork.ProductSellerMappings.Update(mapping);
            await _unitOfWork.SaveChangesAsync();

            // Reload with related data
            var mappingWithRelated = await _unitOfWork.ProductSellerMappings.GetByIdWithConfigurationAsync(mappingId);
            var mappingDto = _mappingService.MapToDto(mappingWithRelated!);

            _logger.LogInformation("Updated product seller mapping {MappingId}", mappingId);
            return Result<ProductSellerMappingDto>.Success(mappingDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product seller mapping {MappingId}", mappingId);
            return Result<ProductSellerMappingDto>.Failure("An error occurred while updating the mapping.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteMappingAsync(Guid mappingId)
    {
        try
        {
            var mapping = await _unitOfWork.ProductSellerMappings.GetByIdAsync(mappingId);
            if (mapping == null)
            {
                return Result.Failure("Mapping not found.", "RESOURCE_NOT_FOUND");
            }

            _unitOfWork.ProductSellerMappings.Remove(mapping);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted product seller mapping {MappingId}", mappingId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product seller mapping {MappingId}", mappingId);
            return Result.Failure("An error occurred while deleting the mapping.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<ProductSellerMappingDto>>> GetAllMappingsAsync()
    {
        try
        {
            var mappings = await _unitOfWork.ProductSellerMappings.GetAllAsync();
            var mappingDtos = mappings.Select(_mappingService.MapToDto);
            return Result<IEnumerable<ProductSellerMappingDto>>.Success(mappingDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all mappings");
            return Result<IEnumerable<ProductSellerMappingDto>>.Failure("An error occurred while retrieving all mappings.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<ProductSellerMappingDto>>> BulkUpdateProductMappingsAsync(Guid productId, ProductSellerMappingBulkUpdateDto bulkUpdateDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == productId);
            if (!productExists)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<IEnumerable<ProductSellerMappingDto>>.Failure("Product not found.", "PRODUCT_NOT_FOUND");
            }

            var resultMappings = new List<ProductSellerMappingDto>();

            // Process deletions first
            foreach (var deleteId in bulkUpdateDto.DeleteIds)
            {
                var mappingToDelete = await _unitOfWork.ProductSellerMappings.GetByIdAsync(deleteId);
                if (mappingToDelete != null && mappingToDelete.CanonicalProductId == productId)
                {
                    _unitOfWork.ProductSellerMappings.Remove(mappingToDelete);
                    _logger.LogInformation("Deleted product seller mapping {MappingId} in bulk operation", deleteId);
                }
            }

            // Process updates
            foreach (var updateDto in bulkUpdateDto.Update)
            {
                var mappingToUpdate = await _unitOfWork.ProductSellerMappings.GetByIdAsync(updateDto.MappingId);
                if (mappingToUpdate == null || mappingToUpdate.CanonicalProductId != productId)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<IEnumerable<ProductSellerMappingDto>>.Failure($"Mapping {updateDto.MappingId} not found or doesn't belong to this product.", "MAPPING_NOT_FOUND");
                }

                // Validate site configuration exists if being updated
                if (updateDto.SiteConfigId.HasValue)
                {
                    var configExists = await _unitOfWork.ScraperSiteConfigurations.ExistsAsync(s => s.SiteConfigId == updateDto.SiteConfigId.Value);
                    if (!configExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<IEnumerable<ProductSellerMappingDto>>.Failure("Site configuration not found.", "SITE_CONFIG_NOT_FOUND");
                    }
                }

                // Update mapping
                if (!string.IsNullOrWhiteSpace(updateDto.SellerName))
                    mappingToUpdate.SellerName = updateDto.SellerName;
                if (!string.IsNullOrWhiteSpace(updateDto.ExactProductUrl))
                    mappingToUpdate.ExactProductUrl = updateDto.ExactProductUrl;
                if (updateDto.IsActiveForScraping.HasValue)
                    mappingToUpdate.IsActiveForScraping = updateDto.IsActiveForScraping.Value;
                if (updateDto.ScrapingFrequencyOverride != null)
                    mappingToUpdate.ScrapingFrequencyOverride = updateDto.ScrapingFrequencyOverride;
                if (updateDto.SiteConfigId != null)
                    mappingToUpdate.SiteConfigId = updateDto.SiteConfigId;

                mappingToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.ProductSellerMappings.Update(mappingToUpdate);

                _logger.LogInformation("Updated product seller mapping {MappingId} in bulk operation", updateDto.MappingId);
            }

            // Process creates
            foreach (var createDto in bulkUpdateDto.Create)
            {
                // Validate site configuration exists if provided
                if (createDto.SiteConfigId.HasValue)
                {
                    var configExists = await _unitOfWork.ScraperSiteConfigurations.ExistsAsync(s => s.SiteConfigId == createDto.SiteConfigId.Value);
                    if (!configExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<IEnumerable<ProductSellerMappingDto>>.Failure("Site configuration not found.", "SITE_CONFIG_NOT_FOUND");
                    }
                }

                // Check for duplicate mapping
                var existingMapping = await _unitOfWork.ProductSellerMappings.FirstOrDefaultAsync(m =>
                    m.CanonicalProductId == productId &&
                    m.SellerName == createDto.SellerName &&
                    m.ExactProductUrl == createDto.ExactProductUrl);

                if (existingMapping != null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<IEnumerable<ProductSellerMappingDto>>.Failure($"A mapping with seller '{createDto.SellerName}' and URL '{createDto.ExactProductUrl}' already exists.", "MAPPING_EXISTS");
                }

                var newMapping = new ProductSellerMapping
                {
                    MappingId = Guid.NewGuid(),
                    CanonicalProductId = productId,
                    SellerName = createDto.SellerName,
                    ExactProductUrl = createDto.ExactProductUrl,
                    IsActiveForScraping = createDto.IsActiveForScraping,
                    ScrapingFrequencyOverride = createDto.ScrapingFrequencyOverride,
                    SiteConfigId = createDto.SiteConfigId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    NextScrapeAt = DateTimeOffset.UtcNow // Initialize for immediate scraping
                };

                await _unitOfWork.ProductSellerMappings.AddAsync(newMapping);
                _logger.LogInformation("Created product seller mapping {MappingId} for product {ProductId} in bulk operation", newMapping.MappingId, productId);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Get updated mappings for the product to return
            var updatedMappings = await _unitOfWork.ProductSellerMappings.GetByProductIdAsync(productId);
            var mappingDtos = updatedMappings.Select(_mappingService.MapToDto);

            _logger.LogInformation("Completed bulk update for product {ProductId}: {CreateCount} created, {UpdateCount} updated, {DeleteCount} deleted", 
                productId, bulkUpdateDto.Create.Count, bulkUpdateDto.Update.Count, bulkUpdateDto.DeleteIds.Count);

            return Result<IEnumerable<ProductSellerMappingDto>>.Success(mappingDtos);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error performing bulk update for product {ProductId}", productId);
            return Result<IEnumerable<ProductSellerMappingDto>>.Failure("An error occurred while performing bulk update.", "INTERNAL_ERROR");
        }
    }
}
