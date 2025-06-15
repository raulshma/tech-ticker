using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;

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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createDto)
    {
        var result = await _categoryService.CreateCategoryAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
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
    public async Task<IActionResult> GetCategory(string categoryIdOrSlug)
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateCategoryDto updateDto)
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
    public async Task<IActionResult> DeleteCategory(Guid categoryId)
    {
        var result = await _categoryService.DeleteCategoryAsync(categoryId);
        return HandleResult(result);
    }
}
