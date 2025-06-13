using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    /// Service for product operations
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ProductDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request)
        {
            try
            {
                // Validate category exists
                if (!await CategoryExistsAsync(request.CategoryId))
                {
                    return Result<ProductResponse>.Failure(
                        "Category not found", 
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Validate SKU is unique
                if (!string.IsNullOrEmpty(request.SKU) && await IsSkuInUseAsync(request.SKU))
                {
                    return Result<ProductResponse>.Failure(
                        "SKU is already in use", 
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                var product = new Product
                {
                    ProductId = Guid.NewGuid(),
                    Name = request.Name,
                    Manufacturer = request.Manufacturer,
                    ModelNumber = request.ModelNumber,
                    SKU = request.SKU,
                    CategoryId = request.CategoryId,
                    Description = request.Description,
                    Specifications = request.Specifications?.GetRawText(),
                    IsActive = request.IsActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created successfully with ID: {ProductId}", product.ProductId);

                var response = await GetProductByIdAsync(product.ProductId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return Result<ProductResponse>.Failure(ex);
            }
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return Result<ProductResponse>.Failure(
                        "Product not found", 
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var response = MapToProductResponse(product);
                return Result<ProductResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID: {ProductId}", productId);
                return Result<ProductResponse>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<ProductResponse>>> GetProductsAsync(ProductQueryRequest query)
        {
            try
            {
                var queryable = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // Apply category filter
                if (!string.IsNullOrEmpty(query.CategoryId))
                {
                    // Try to parse as Guid first, if fails, treat as slug
                    if (Guid.TryParse(query.CategoryId, out var categoryGuid))
                    {
                        queryable = queryable.Where(p => p.CategoryId == categoryGuid);
                    }
                    else
                    {
                        queryable = queryable.Where(p => p.Category.Slug == query.CategoryId);
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    queryable = queryable.Where(p => 
                        p.Name.ToLower().Contains(searchTerm) ||
                        (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(searchTerm)) ||
                        (p.ModelNumber != null && p.ModelNumber.ToLower().Contains(searchTerm)) ||
                        (p.SKU != null && p.SKU.ToLower().Contains(searchTerm)));
                }

                // Apply active filter
                if (query.IsActive.HasValue)
                {
                    queryable = queryable.Where(p => p.IsActive == query.IsActive.Value);
                }

                // Get total count
                var totalCount = await queryable.CountAsync();

                // Apply pagination
                var skip = (query.PageNumber - 1) * query.PageSize;
                var products = await queryable
                    .OrderBy(p => p.Name)
                    .Skip(skip)
                    .Take(query.PageSize)
                    .ToListAsync();

                var responses = products.Select(MapToProductResponse).ToList();

                var pagedResponse = PagedResponse<ProductResponse>.SuccessResult(
                    responses, 
                    query.PageNumber, 
                    query.PageSize, 
                    totalCount);

                return Result<PagedResponse<ProductResponse>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return Result<PagedResponse<ProductResponse>>.Failure(ex);
            }
        }

        public async Task<Result<ProductResponse>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Result<ProductResponse>.Failure(
                        "Product not found", 
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Validate category exists
                if (!await CategoryExistsAsync(request.CategoryId))
                {
                    return Result<ProductResponse>.Failure(
                        "Category not found", 
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Validate SKU is unique (excluding current product)
                if (!string.IsNullOrEmpty(request.SKU) && await IsSkuInUseAsync(request.SKU, productId))
                {
                    return Result<ProductResponse>.Failure(
                        "SKU is already in use", 
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                // Update product properties
                product.Name = request.Name;
                product.Manufacturer = request.Manufacturer;
                product.ModelNumber = request.ModelNumber;
                product.SKU = request.SKU;
                product.CategoryId = request.CategoryId;
                product.Description = request.Description;
                product.Specifications = request.Specifications?.GetRawText();
                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated successfully with ID: {ProductId}", productId);

                var response = await GetProductByIdAsync(productId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", productId);
                return Result<ProductResponse>.Failure(ex);
            }
        }

        public async Task<Result> DeleteProductAsync(Guid productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Result.Failure(
                        "Product not found", 
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product deleted successfully with ID: {ProductId}", productId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", productId);
                return Result.Failure(ex);
            }
        }

        public async Task<bool> ProductExistsAsync(Guid productId)
        {
            return await _context.Products.AnyAsync(p => p.ProductId == productId);
        }

        public async Task<bool> IsSkuInUseAsync(string sku, Guid? excludeProductId = null)
        {
            if (string.IsNullOrEmpty(sku))
                return false;

            var query = _context.Products.Where(p => p.SKU == sku);
            
            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.ProductId != excludeProductId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> CategoryExistsAsync(Guid categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        private ProductResponse MapToProductResponse(Product product)
        {
            JsonElement? specifications = null;
            if (!string.IsNullOrEmpty(product.Specifications))
            {
                try
                {
                    specifications = JsonSerializer.Deserialize<JsonElement>(product.Specifications);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse specifications JSON for product {ProductId}", product.ProductId);
                }
            }

            return new ProductResponse
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Manufacturer = product.Manufacturer,
                ModelNumber = product.ModelNumber,
                SKU = product.SKU,
                CategoryId = product.CategoryId,
                Category = product.Category != null ? new CategoryResponse
                {
                    CategoryId = product.Category.CategoryId,
                    Name = product.Category.Name,
                    Slug = product.Category.Slug,
                    Description = product.Category.Description
                } : null,
                Description = product.Description,
                Specifications = specifications,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}
