using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for Product operations
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> CreateProductAsync(CreateProductDto createDto)
    {
        try
        {
            // Validate category exists
            var category = await _unitOfWork.Categories.GetByIdAsync(createDto.CategoryId);
            if (category == null)
            {
                return Result<ProductDto>.FailureResult("Category not found.", "CATEGORY_NOT_FOUND");
            }

            // Check if SKU already exists (if provided)
            if (!string.IsNullOrWhiteSpace(createDto.SKU))
            {
                var existingProduct = await _unitOfWork.Products.FirstOrDefaultAsync(p => p.SKU == createDto.SKU);
                if (existingProduct != null)
                {
                    return Result<ProductDto>.FailureResult("A product with this SKU already exists.", "SKU_EXISTS");
                }
            }

            var product = _mappingService.MapToEntity(createDto);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Reload with category for response
            var productWithCategory = await _unitOfWork.Products.GetByIdWithCategoryAsync(product.ProductId);
            var productDto = _mappingService.MapToDto(productWithCategory!);

            _logger.LogInformation("Created product {ProductId} with name {Name}", product.ProductId, product.Name);
            return Result<ProductDto>.SuccessResult(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with name {Name}", createDto.Name);
            return Result<ProductDto>.FailureResult("An error occurred while creating the product.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<PagedResponse<ProductDto>>> GetProductsAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            // Validate category exists if provided
            if (categoryId.HasValue)
            {
                var categoryExists = await _unitOfWork.Categories.ExistsAsync(c => c.CategoryId == categoryId.Value);
                if (!categoryExists)
                {
                    return Result<PagedResponse<ProductDto>>.FailureResult("Category not found.", "CATEGORY_NOT_FOUND");
                }
            }

            var (products, totalCount) = await _unitOfWork.Products.GetProductsAsync(
                categoryId, search, pageNumber, pageSize);

            var productDtos = products.Select(_mappingService.MapToDto);

            var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
                productDtos, pageNumber, pageSize, totalCount);

            return Result<PagedResponse<ProductDto>>.SuccessResult(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products with categoryId {CategoryId}, search {Search}", categoryId, search);
            return Result<PagedResponse<ProductDto>>.FailureResult("An error occurred while retrieving products.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ProductDto>> GetProductByIdAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdWithCategoryAsync(productId);
            if (product == null)
            {
                return Result<ProductDto>.FailureResult("Product not found.", "RESOURCE_NOT_FOUND");
            }

            var productDto = _mappingService.MapToDto(product);
            return Result<ProductDto>.SuccessResult(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", productId);
            return Result<ProductDto>.FailureResult("An error occurred while retrieving the product.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(Guid productId, UpdateProductDto updateDto)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<ProductDto>.FailureResult("Product not found.", "RESOURCE_NOT_FOUND");
            }

            // Validate category exists if being updated
            if (updateDto.CategoryId.HasValue)
            {
                var categoryExists = await _unitOfWork.Categories.ExistsAsync(c => c.CategoryId == updateDto.CategoryId.Value);
                if (!categoryExists)
                {
                    return Result<ProductDto>.FailureResult("Category not found.", "CATEGORY_NOT_FOUND");
                }
            }

            // Check if new SKU conflicts with existing products
            if (!string.IsNullOrWhiteSpace(updateDto.SKU) && updateDto.SKU != product.SKU)
            {
                var existingProduct = await _unitOfWork.Products.FirstOrDefaultAsync(p => p.SKU == updateDto.SKU);
                if (existingProduct != null)
                {
                    return Result<ProductDto>.FailureResult("A product with this SKU already exists.", "SKU_EXISTS");
                }
            }

            _mappingService.MapToEntity(updateDto, product);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // Reload with category for response
            var productWithCategory = await _unitOfWork.Products.GetByIdWithCategoryAsync(productId);
            var productDto = _mappingService.MapToDto(productWithCategory!);

            _logger.LogInformation("Updated product {ProductId}", productId);
            return Result<ProductDto>.SuccessResult(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", productId);
            return Result<ProductDto>.FailureResult("An error occurred while updating the product.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteProductAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result.FailureResult("Product not found.", "RESOURCE_NOT_FOUND");
            }

            // Soft delete by setting IsActive to false
            product.IsActive = false;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Soft deleted product {ProductId}", productId);
            return Result.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", productId);
            return Result.FailureResult("An error occurred while deleting the product.", "INTERNAL_ERROR");
        }
    }
}
