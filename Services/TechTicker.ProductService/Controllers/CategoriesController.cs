using Microsoft.AspNetCore.Mvc;
using TechTicker.ProductService.DTOs;
using TechTicker.ProductService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ProductService.Controllers
{
    /// <summary>
    /// Controller for managing product categories
    /// </summary>
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        /// <summary>
        /// Initializes a new instance of the CategoriesController
        /// </summary>
        /// <param name="categoryService">The category service</param>
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }        /// <summary>
        /// Creates a new category
        /// </summary>
        /// <param name="request">The category creation data</param>
        /// <returns>The created category</returns>
        /// <response code="201">Category created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="409">Category with same name or slug already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var result = await _categoryService.CreateCategoryAsync(request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }            return CreatedAtAction(
                nameof(GetCategory), 
                new { categoryIdOrSlug = result.Data!.CategoryId }, 
                result.Data
            );
        }

        /// <summary>
        /// Gets all categories with optional filtering, searching, and pagination
        /// </summary>
        /// <param name="query">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of categories</returns>
        /// <response code="200">Categories retrieved successfully</response>
        /// <response code="400">Invalid query parameters</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<CategoryResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> GetCategories([FromQuery] CategoryQueryRequest query)
        {
            var result = await _categoryService.GetCategoriesAsync(query);
              if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return OkPagedDirect(result.Data!);
        }

        /// <summary>
        /// Gets a specific category by ID or slug
        /// </summary>
        /// <param name="categoryIdOrSlug">The category ID (GUID) or slug (string)</param>
        /// <returns>The category details</returns>
        /// <response code="200">Category found</response>
        /// <response code="404">Category not found</response>
        [HttpGet("{categoryIdOrSlug}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCategory(string categoryIdOrSlug)
        {
            var result = await _categoryService.GetCategoryByIdOrSlugAsync(categoryIdOrSlug, includeProductCount: true);
              if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok(result.Data!, "Category retrieved successfully");
        }

        /// <summary>
        /// Updates an existing category
        /// </summary>
        /// <param name="categoryId">The category ID</param>
        /// <param name="request">The category update data</param>
        /// <returns>The updated category</returns>
        /// <response code="200">Category updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="404">Category not found</response>
        /// <response code="409">Category with same name or slug already exists</response>
        [HttpPut("{categoryId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateCategoryRequest request)
        {
            var result = await _categoryService.UpdateCategoryAsync(categoryId, request);
              if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok(result.Data!, "Category updated successfully");
        }

        /// <summary>
        /// Deletes a category
        /// </summary>
        /// <param name="categoryId">The category ID</param>
        /// <returns>Confirmation of deletion</returns>
        /// <response code="200">Category deleted successfully</response>
        /// <response code="404">Category not found</response>
        /// <response code="409">Cannot delete category with associated products</response>
        [HttpDelete("{categoryId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            var result = await _categoryService.DeleteCategoryAsync(categoryId);
              if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok("Category deleted successfully");
        }
    }
}
