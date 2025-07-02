using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Product Comparison operations
/// </summary>
[Route("api/product-comparison")]
[ApiController]
public class ProductComparisonController(IProductComparisonService productComparisonService) : BaseApiController
{
    private readonly IProductComparisonService _productComparisonService = productComparisonService;

    /// <summary>
    /// Compare two products with comprehensive analysis including specifications, pricing, and AI recommendations
    /// </summary>
    /// <param name="request">Comparison request containing product IDs and optional parameters</param>
    /// <returns>Complete comparison result with detailed analysis</returns>
    /// <response code="200">Comparison completed successfully</response>
    /// <response code="400">Invalid request parameters or validation errors</response>
    /// <response code="404">One or both products not found</response>
    /// <response code="422">Products cannot be compared (different categories, etc.)</response>
    [HttpPost("compare", Name = "CompareProducts")]
    [RequirePermission(Permissions.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<ProductComparisonResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 422)]
    public async Task<ActionResult<ApiResponse<ProductComparisonResultDto>>> CompareProducts([FromBody] CompareProductsRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var response = ApiResponse.FailureResult(string.Join("; ", errors), 400);
                response.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<ProductComparisonResultDto>>) new BadRequestObjectResult(response);
            }

            var result = await _productComparisonService.CompareProductsAsync(request);
            if (result.IsSuccess)
            {
                var response = ApiResponse<ProductComparisonResultDto>.SuccessResult(result.Data!);
                response.CorrelationId = CorrelationId;
                return new OkObjectResult(response);
            }
            else
            {
                var errorResponse = ApiResponse.FailureResult(result.ErrorMessage ?? "Error", result.ErrorCode == "PRODUCT_NOT_FOUND" ? 404 : result.ErrorCode == "CATEGORY_MISMATCH" ? 400 : result.ErrorCode == "INVALID_INPUT" ? 400 : result.ErrorCode == "INTERNAL_ERROR" ? 500 : 400);
                errorResponse.CorrelationId = CorrelationId;
                if (result.ErrorCode == "PRODUCT_NOT_FOUND")
                    return (ActionResult<ApiResponse<ProductComparisonResultDto>>) new NotFoundObjectResult(errorResponse);
                if (result.ErrorCode == "INTERNAL_ERROR")
                    return (ActionResult<ApiResponse<ProductComparisonResultDto>>) new ObjectResult(errorResponse) { StatusCode = 500 };
                return (ActionResult<ApiResponse<ProductComparisonResultDto>>) new BadRequestObjectResult(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var response = ApiResponse.FailureResult($"An error occurred: {ex.Message}", 500);
            response.CorrelationId = CorrelationId;
            return (ActionResult<ApiResponse<ProductComparisonResultDto>>) new ObjectResult(response) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Validate that two products can be compared (same category, both exist)
    /// </summary>
    /// <param name="productId1">First product ID to validate</param>
    /// <param name="productId2">Second product ID to validate</param>
    /// <returns>Validation result indicating whether products can be compared</returns>
    /// <response code="200">Validation completed successfully</response>
    /// <response code="400">Invalid product IDs</response>
    /// <response code="404">One or both products not found</response>
    [HttpGet("validate", Name = "ValidateProductsForComparison")]
    [RequirePermission(Permissions.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateProductsForComparison(
        [FromQuery, Required] Guid productId1,
        [FromQuery, Required] Guid productId2)
    {
        try
        {
            if (productId1 == Guid.Empty)
            {
                var response = ApiResponse.FailureResult("productId1 cannot be empty", 400);
                response.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<bool>>) new BadRequestObjectResult(response);
            }
            if (productId2 == Guid.Empty)
            {
                var response = ApiResponse.FailureResult("productId2 cannot be empty", 400);
                response.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<bool>>) new BadRequestObjectResult(response);
            }
            if (productId1 == productId2)
            {
                var response = ApiResponse.FailureResult("Cannot compare a product with itself. Product IDs must be different", 400);
                response.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<bool>>) new BadRequestObjectResult(response);
            }
            var result = await _productComparisonService.ValidateProductsForComparisonAsync(productId1, productId2);
            if (result.IsSuccess)
            {
                var response = ApiResponse<bool>.SuccessResult(result.Data!);
                response.CorrelationId = CorrelationId;
                return new OkObjectResult(response);
            }
            else
            {
                var errorResponse = ApiResponse.FailureResult(result.ErrorMessage ?? "Error", result.ErrorCode == "PRODUCT_NOT_FOUND" ? 404 : 400);
                errorResponse.CorrelationId = CorrelationId;
                if (result.ErrorCode == "PRODUCT_NOT_FOUND")
                    return (ActionResult<ApiResponse<bool>>) new NotFoundObjectResult(errorResponse);
                return (ActionResult<ApiResponse<bool>>) new BadRequestObjectResult(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var response = ApiResponse.FailureResult($"An error occurred: {ex.Message}", 500);
            response.CorrelationId = CorrelationId;
            return (ActionResult<ApiResponse<bool>>) new ObjectResult(response) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Get products that can be compared with a specific product (same category)
    /// </summary>
    /// <param name="id">Base product ID to find comparable products for</param>
    /// <param name="search">Optional search term to filter comparable products</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 50)</param>
    /// <returns>Paginated list of products that can be compared with the specified product</returns>
    /// <response code="200">Comparable products retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="404">Base product not found</response>
    [HttpGet("comparable-products/{id:guid}", Name = "GetComparableProducts")]
    [RequirePermission(Permissions.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> GetComparableProducts(
        Guid id,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (id == Guid.Empty)
            {
                var errorResponse = ApiResponse.FailureResult("Product ID cannot be empty", 400);
                errorResponse.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new BadRequestObjectResult(errorResponse);
            }
            if (page < 1)
            {
                var errorResponse = ApiResponse.FailureResult("Page number must be greater than 0", 400);
                errorResponse.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new BadRequestObjectResult(errorResponse);
            }
            if (pageSize < 1 || pageSize > 50)
            {
                var errorResponse = ApiResponse.FailureResult("Page size must be between 1 and 50", 400);
                errorResponse.CorrelationId = CorrelationId;
                return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new BadRequestObjectResult(errorResponse);
            }
            var result = await _productComparisonService.GetComparableProductsAsync(id, search, page, pageSize);
            if (result.IsSuccess)
            {
                var response = ApiResponse<PagedResponse<ProductDto>>.SuccessResult(result.Data!);
                response.CorrelationId = CorrelationId;
                return new OkObjectResult(response);
            }
            else
            {
                var errorResponse = ApiResponse.FailureResult(result.ErrorMessage ?? "Error", result.ErrorCode == "PRODUCT_NOT_FOUND" ? 404 : 400);
                errorResponse.CorrelationId = CorrelationId;
                if (result.ErrorCode == "PRODUCT_NOT_FOUND")
                    return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new NotFoundObjectResult(errorResponse);
                return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new BadRequestObjectResult(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse.FailureResult($"An error occurred: {ex.Message}", 500);
            errorResponse.CorrelationId = CorrelationId;
            return (ActionResult<ApiResponse<PagedResponse<ProductDto>>>) new ObjectResult(errorResponse) { StatusCode = 500 };
        }
    }
}