using Microsoft.EntityFrameworkCore;
using TechTicker.ProductService.Data;
using TechTicker.ProductService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Exceptions;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductService.Services
{
    /// <summary>
    /// Service for category operations
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ProductDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
        {
            try
            {
                var slug = request.GetSlug();

                // Validate name is unique
                if (await IsNameInUseAsync(request.Name))
                {
                    return Result<CategoryResponse>.Failure(
                        "Category name is already in use",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                // Validate slug is unique
                if (await IsSlugInUseAsync(slug))
                {
                    return Result<CategoryResponse>.Failure(
                        "Category slug is already in use",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                var category = new Category
                {
                    CategoryId = Guid.NewGuid(),
                    Name = request.Name,
                    Slug = slug,
                    Description = request.Description,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created successfully with ID: {CategoryId}", category.CategoryId);

                var response = await GetCategoryByIdOrSlugAsync(category.CategoryId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return Result<CategoryResponse>.Failure(ex);
            }
        }

        public async Task<Result<CategoryResponse>> GetCategoryByIdOrSlugAsync(string categoryIdOrSlug, bool includeProductCount = true)
        {
            try
            {
                Category? category = null;

                // Try to parse as Guid first
                if (Guid.TryParse(categoryIdOrSlug, out var categoryGuid))
                {
                    category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryGuid);
                }
                else
                {
                    // Search by slug
                    category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == categoryIdOrSlug);
                }

                if (category == null)
                {
                    return Result<CategoryResponse>.Failure(
                        "Category not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var response = await MapToCategoryResponseAsync(category, includeProductCount);
                return Result<CategoryResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID or slug: {CategoryIdOrSlug}", categoryIdOrSlug);
                return Result<CategoryResponse>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<CategoryResponse>>> GetCategoriesAsync(CategoryQueryRequest query)
        {
            try
            {
                var queryable = _context.Categories.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    queryable = queryable.Where(c => 
                        c.Name.ToLower().Contains(searchTerm) ||
                        (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
                }

                // Get total count
                var totalCount = await queryable.CountAsync();

                // Apply pagination
                var skip = (query.PageNumber - 1) * query.PageSize;
                var categories = await queryable
                    .OrderBy(c => c.Name)
                    .Skip(skip)
                    .Take(query.PageSize)
                    .ToListAsync();

                var responses = new List<CategoryResponse>();
                foreach (var category in categories)
                {
                    var response = await MapToCategoryResponseAsync(category, query.IncludeProductCount);
                    responses.Add(response);
                }

                var pagedResponse = PagedResponse<CategoryResponse>.SuccessResult(
                    responses,
                    query.PageNumber,
                    query.PageSize,
                    totalCount);

                return Result<PagedResponse<CategoryResponse>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return Result<PagedResponse<CategoryResponse>>.Failure(ex);
            }
        }

        public async Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return Result<CategoryResponse>.Failure(
                        "Category not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Validate name is unique (excluding current category)
                if (await IsNameInUseAsync(request.Name, categoryId))
                {
                    return Result<CategoryResponse>.Failure(
                        "Category name is already in use",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                // Validate slug is unique (excluding current category)
                if (await IsSlugInUseAsync(request.Slug, categoryId))
                {
                    return Result<CategoryResponse>.Failure(
                        "Category slug is already in use",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                // Update category properties
                category.Name = request.Name;
                category.Slug = request.Slug;
                category.Description = request.Description;
                category.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated successfully with ID: {CategoryId}", categoryId);

                var response = await GetCategoryByIdOrSlugAsync(categoryId.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID: {CategoryId}", categoryId);
                return Result<CategoryResponse>.Failure(ex);
            }
        }

        public async Task<Result> DeleteCategoryAsync(Guid categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

                if (category == null)
                {
                    return Result.Failure(
                        "Category not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Check if category has products
                if (category.Products.Any())
                {
                    return Result.Failure(
                        "Cannot delete category that contains products",
                        ApplicationConstants.ErrorCodes.BusinessRuleViolation);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category deleted successfully with ID: {CategoryId}", categoryId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID: {CategoryId}", categoryId);
                return Result.Failure(ex);
            }
        }

        public async Task<bool> CategoryExistsAsync(Guid categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> IsSlugInUseAsync(string slug, Guid? excludeCategoryId = null)
        {
            if (string.IsNullOrEmpty(slug))
                return false;

            var query = _context.Categories.Where(c => c.Slug == slug);

            if (excludeCategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> IsNameInUseAsync(string name, Guid? excludeCategoryId = null)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            var query = _context.Categories.Where(c => c.Name == name);

            if (excludeCategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetProductCountAsync(Guid categoryId)
        {
            return await _context.Products.CountAsync(p => p.CategoryId == categoryId);
        }

        private async Task<CategoryResponse> MapToCategoryResponseAsync(Category category, bool includeProductCount = true)
        {
            var response = new CategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            if (includeProductCount)
            {
                response.ProductCount = await GetProductCountAsync(category.CategoryId);
            }

            return response;
        }
    }
}
