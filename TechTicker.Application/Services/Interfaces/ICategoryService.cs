using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Category operations
/// </summary>
public interface ICategoryService
{
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createDto);
    Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();
    Task<Result<CategoryDto>> GetCategoryByIdOrSlugAsync(string idOrSlug);
    Task<Result<CategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryDto updateDto);
    Task<Result> DeleteCategoryAsync(Guid categoryId);
}
