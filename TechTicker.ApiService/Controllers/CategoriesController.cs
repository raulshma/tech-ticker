using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Category operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="createDto">Category creation data</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [RequirePermission(Permissions.ProductsManageCategories)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto createDto)
    {
        var result = await _categoryService.CreateCategoryAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet]
    [RequirePermission(Permissions.ProductsRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetCategories()
    {
        var result = await _categoryService.GetAllCategoriesAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get category by ID or slug
    /// </summary>
    /// <param name="categoryIdOrSlug">Category ID or slug</param>
    /// <returns>Category details</returns>
    [HttpGet("{categoryIdOrSlug}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(string categoryIdOrSlug)
    {
        var result = await _categoryService.GetCategoryByIdOrSlugAsync(categoryIdOrSlug);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="updateDto">Category update data</param>
    /// <returns>Updated category</returns>
    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(Guid categoryId, [FromBody] UpdateCategoryDto updateDto)
    {
        var result = await _categoryService.UpdateCategoryAsync(categoryId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(Guid categoryId)
    {
        var result = await _categoryService.DeleteCategoryAsync(categoryId);
        return HandleResult(result);
    }
}
