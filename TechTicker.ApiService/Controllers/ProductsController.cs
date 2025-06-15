using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Product operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IPriceHistoryService _priceHistoryService;

    public ProductsController(
        IProductService productService,
        IPriceHistoryService priceHistoryService)
    {
        _productService = productService;
        _priceHistoryService = priceHistoryService;
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="createDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createDto)
    {
        var result = await _productService.CreateProductAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get products with optional filtering and pagination
    /// </summary>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="search">Optional search term</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of products</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _productService.GetProductsAsync(categoryId, search, page, pageSize);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid productId)
    {
        var result = await _productService.GetProductByIdAsync(productId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="updateDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid productId, [FromBody] UpdateProductDto updateDto)
    {
        var result = await _productService.UpdateProductAsync(productId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a product (soft delete)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteProduct(Guid productId)
    {
        var result = await _productService.DeleteProductAsync(productId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get price history for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="sellerName">Optional seller filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="limit">Optional limit on number of records</param>
    /// <returns>Price history</returns>
    [HttpGet("{productId:guid}/price-history")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PriceHistoryDto>>>> GetPriceHistory(
        Guid productId,
        [FromQuery] string? sellerName = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] int? limit = null)
    {
        var queryDto = new PriceHistoryQueryDto
        {
            SellerName = sellerName,
            StartDate = startDate,
            EndDate = endDate,
            Limit = limit
        };

        var result = await _priceHistoryService.GetPriceHistoryAsync(productId, queryDto);
        return HandleResult(result);
    }
}
