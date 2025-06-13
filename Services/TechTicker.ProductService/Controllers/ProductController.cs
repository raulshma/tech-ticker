using Microsoft.AspNetCore.Mvc;
using TechTicker.ProductService.DTOs;
using TechTicker.ProductService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ProductService.Controllers
{
    /// <summary>
    /// Controller for managing products
    /// </summary>
    [Route("api/[controller]")]
    public class ProductController : BaseApiController
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new product (Admin only)
        /// </summary>
        /// <param name="request">Product creation request</param>
        /// <returns>Created product details</returns>
        /// <response code="201">Product created successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Category not found</response>
        /// <response code="409">SKU already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            _logger.LogInformation("Creating new product: {ProductName}", request.Name);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(errors, "Validation failed");
            }

            var result = await _productService.CreateProductAsync(request);
            
            if (result.IsSuccess)
            {
                return Created(result.Data, "Product created successfully");
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Gets a list of products with optional filtering
        /// </summary>
        /// <param name="categoryId">Category ID or slug to filter by</param>
        /// <param name="search">Search term for product name, manufacturer, or model number</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size (max 100)</param>
        /// <returns>Paginated list of products</returns>
        /// <response code="200">Products retrieved successfully</response>
        /// <response code="400">Invalid query parameters</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new ProductQueryRequest
            {
                CategoryId = categoryId,
                Search = search,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = Math.Min(pageSize, 100) // Cap at 100
            };

            _logger.LogInformation("Retrieving products with filters - Category: {CategoryId}, Search: {Search}, Page: {PageNumber}, Size: {PageSize}", 
                categoryId, search, pageNumber, pageSize);

            var result = await _productService.GetProductsAsync(query);            if (result.IsSuccess)
            {
                return OkPagedDirect(result.Data!);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Gets a product by ID
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Product details with category information</returns>
        /// <response code="200">Product retrieved successfully</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{productId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProduct(Guid productId)
        {
            _logger.LogInformation("Retrieving product with ID: {ProductId}", productId);

            var result = await _productService.GetProductByIdAsync(productId);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates an existing product (Admin only)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="request">Product update request</param>
        /// <returns>Updated product details</returns>
        /// <response code="200">Product updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Product or category not found</response>
        /// <response code="409">SKU already exists</response>
        [HttpPut("{productId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", productId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(errors, "Validation failed");
            }

            var result = await _productService.UpdateProductAsync(productId, request);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data, "Product updated successfully");
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Deletes a product (Admin only)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Confirmation of deletion</returns>
        /// <response code="200">Product deleted successfully</response>
        /// <response code="404">Product not found</response>
        [HttpDelete("{productId:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", productId);

            var result = await _productService.DeleteProductAsync(productId);
            
            if (result.IsSuccess)
            {
                return Ok("Product deleted successfully");
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Checks if a product exists
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Existence status</returns>
        /// <response code="200">Check completed</response>
        [HttpHead("{productId:guid}")]
        [HttpGet("{productId:guid}/exists")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProductExists(Guid productId)
        {
            var exists = await _productService.ProductExistsAsync(productId);
            return exists ? Ok(new { exists = true }) : NotFound("Product not found");
        }
    }
}
