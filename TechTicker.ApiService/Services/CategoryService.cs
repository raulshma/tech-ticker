using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for Category operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createDto)
    {
        try
        {
            // Check if slug already exists
            var slug = string.IsNullOrWhiteSpace(createDto.Slug) 
                ? TechTicker.Shared.Utilities.StringUtilities.ToSlug(createDto.Name) 
                : createDto.Slug;

            if (await _unitOfWork.Categories.SlugExistsAsync(slug))
            {
                return Result<CategoryDto>.FailureResult("A category with this slug already exists.", "SLUG_EXISTS");
            }

            var category = _mappingService.MapToEntity(createDto);
            category.Slug = slug;

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = _mappingService.MapToDto(category);
            _logger.LogInformation("Created category {CategoryId} with name {Name}", category.CategoryId, category.Name);

            return Result<CategoryDto>.SuccessResult(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category with name {Name}", createDto.Name);
            return Result<CategoryDto>.FailureResult("An error occurred while creating the category.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var categoryDtos = categories.Select(_mappingService.MapToDto);

            return Result<IEnumerable<CategoryDto>>.SuccessResult(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            return Result<IEnumerable<CategoryDto>>.FailureResult("An error occurred while retrieving categories.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<CategoryDto>> GetCategoryByIdOrSlugAsync(string idOrSlug)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdOrSlugAsync(idOrSlug);
            if (category == null)
            {
                return Result<CategoryDto>.FailureResult("Category not found.", "RESOURCE_NOT_FOUND");
            }

            var categoryDto = _mappingService.MapToDto(category);
            return Result<CategoryDto>.SuccessResult(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID or slug {IdOrSlug}", idOrSlug);
            return Result<CategoryDto>.FailureResult("An error occurred while retrieving the category.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<CategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto updateDto)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
            {
                return Result<CategoryDto>.FailureResult("Category not found.", "RESOURCE_NOT_FOUND");
            }

            // Check if new slug conflicts with existing categories
            if (!string.IsNullOrWhiteSpace(updateDto.Slug) && updateDto.Slug != category.Slug)
            {
                if (await _unitOfWork.Categories.SlugExistsAsync(updateDto.Slug, categoryId))
                {
                    return Result<CategoryDto>.FailureResult("A category with this slug already exists.", "SLUG_EXISTS");
                }
            }

            _mappingService.MapToEntity(updateDto, category);
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = _mappingService.MapToDto(category);
            _logger.LogInformation("Updated category {CategoryId}", categoryId);

            return Result<CategoryDto>.SuccessResult(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", categoryId);
            return Result<CategoryDto>.FailureResult("An error occurred while updating the category.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteCategoryAsync(Guid categoryId)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category == null)
            {
                return Result.FailureResult("Category not found.", "RESOURCE_NOT_FOUND");
            }

            // Check if category has products
            var products = await _unitOfWork.Products.GetByCategoryIdAsync(categoryId);
            if (products.Any())
            {
                return Result.FailureResult("Cannot delete category that contains products.", "CATEGORY_HAS_PRODUCTS");
            }

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted category {CategoryId}", categoryId);
            return Result.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", categoryId);
            return Result.FailureResult("An error occurred while deleting the category.", "INTERNAL_ERROR");
        }
    }
}
