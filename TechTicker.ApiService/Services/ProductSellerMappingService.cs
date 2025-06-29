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
}
