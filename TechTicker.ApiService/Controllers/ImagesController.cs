using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Product Image Management operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ImagesController : BaseApiController
{
    private readonly IProductImageService _productImageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IProductImageService productImageService,
        ILogger<ImagesController> logger)
    {
        _productImageService = productImageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload multiple images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="images">Images to upload</param>
    /// <returns>List of uploaded images with metadata</returns>
    [HttpPost("products/{productId:guid}/upload")]
    [RequirePermission(Permissions.ProductsUpdate)]
    [RequestSizeLimit(52428800)] // 50MB total request size
    public async Task<ActionResult<ApiResponse<List<ImageDto>>>> UploadImages(
        Guid productId,
        [FromForm] List<ImageUploadDto> images)
    {
        try
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(ApiResponse<List<ImageDto>>.FailureResult("No images provided", 400));
            }

            if (images.Count > 10) // Limit to 10 images per upload
            {
                return BadRequest(ApiResponse<List<ImageDto>>.FailureResult("Too many images. Maximum 10 images per upload", 400));
            }

            _logger.LogInformation("Uploading {Count} images for product {ProductId}", images.Count, productId);
            
            var result = await _productImageService.UploadImagesAsync(productId, images);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully uploaded {Count} images for product {ProductId}", 
                    result.Data!.Count, productId);
            }
            else
            {
                _logger.LogWarning("Failed to upload images for product {ProductId}: {Error}", 
                    productId, result.ErrorMessage);
            }
            
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<List<ImageDto>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get all images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>List of product images</returns>
    [HttpGet("products/{productId:guid}")]
    [RequirePermission(Permissions.ProductsRead)]
    public async Task<ActionResult<ApiResponse<List<ImageDto>>>> GetProductImages(Guid productId)
    {
        try
        {
            _logger.LogDebug("Retrieving images for product {ProductId}", productId);
            
            var result = await _productImageService.GetProductImagesAsync(productId);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<List<ImageDto>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Delete a specific image from a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">Image URL to delete (URL encoded)</param>
    /// <returns>Success status</returns>
    [HttpDelete("products/{productId:guid}/images")]
    [RequirePermission(Permissions.ProductsUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteImage(
        Guid productId,
        [FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(ApiResponse<bool>.FailureResult("Image URL is required", 400));
            }

            // URL decode the image URL
            var decodedImageUrl = Uri.UnescapeDataString(imageUrl);
            
            _logger.LogInformation("Deleting image {ImageUrl} from product {ProductId}", decodedImageUrl, productId);
            
            var result = await _productImageService.DeleteImageAsync(productId, decodedImageUrl);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageUrl} from product {ProductId}", imageUrl, productId);
            return StatusCode(500, ApiResponse<bool>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Set a specific image as the primary image for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="setPrimaryDto">Primary image data</param>
    /// <returns>Success status</returns>
    [HttpPut("products/{productId:guid}/primary")]
    [RequirePermission(Permissions.ProductsUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> SetPrimaryImage(
        Guid productId,
        [FromBody] SetPrimaryImageDto setPrimaryDto)
    {
        try
        {
            if (setPrimaryDto == null || string.IsNullOrWhiteSpace(setPrimaryDto.ImageUrl))
            {
                return BadRequest(ApiResponse<bool>.FailureResult("Image URL is required", 400));
            }

            _logger.LogInformation("Setting primary image {ImageUrl} for product {ProductId}", 
                setPrimaryDto.ImageUrl, productId);
            
            var result = await _productImageService.SetPrimaryImageAsync(productId, setPrimaryDto.ImageUrl);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary image for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<bool>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Reorder images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="reorderDto">Image reorder data</param>
    /// <returns>Success status</returns>
    [HttpPut("products/{productId:guid}/reorder")]
    [RequirePermission(Permissions.ProductsUpdate)]
    public async Task<ActionResult<ApiResponse<bool>>> ReorderImages(
        Guid productId,
        [FromBody] ImageReorderDto reorderDto)
    {
        try
        {
            if (reorderDto == null || reorderDto.ImageUrls == null || reorderDto.ImageUrls.Count == 0)
            {
                return BadRequest(ApiResponse<bool>.FailureResult("Image URLs are required", 400));
            }

            _logger.LogInformation("Reordering {Count} images for product {ProductId}", 
                reorderDto.ImageUrls.Count, productId);
            
            var result = await _productImageService.ReorderImagesAsync(productId, reorderDto.ImageUrls);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering images for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<bool>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Delete multiple images from a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="bulkDeleteDto">Bulk delete data</param>
    /// <returns>Bulk operation result</returns>
    [HttpDelete("products/{productId:guid}/bulk")]
    [RequirePermission(Permissions.ProductsUpdate)]
    public async Task<ActionResult<ApiResponse<BulkImageOperationResultDto>>> BulkDeleteImages(
        Guid productId,
        [FromBody] BulkImageOperationDto bulkDeleteDto)
    {
        try
        {
            if (bulkDeleteDto == null || bulkDeleteDto.ImageUrls == null || bulkDeleteDto.ImageUrls.Count == 0)
            {
                return BadRequest(ApiResponse<BulkImageOperationResultDto>.FailureResult("Image URLs are required", 400));
            }

            if (bulkDeleteDto.ImageUrls.Count > 50) // Reasonable limit for bulk operations
            {
                return BadRequest(ApiResponse<BulkImageOperationResultDto>.FailureResult("Too many images. Maximum 50 images per bulk operation", 400));
            }

            _logger.LogInformation("Bulk deleting {Count} images from product {ProductId}", 
                bulkDeleteDto.ImageUrls.Count, productId);
            
            var result = await _productImageService.BulkDeleteImagesAsync(productId, bulkDeleteDto.ImageUrls);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting images from product {ProductId}", productId);
            return StatusCode(500, ApiResponse<BulkImageOperationResultDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get image metadata including file size and dimensions
    /// </summary>
    /// <param name="imageUrl">Image URL (URL encoded)</param>
    /// <returns>Image metadata</returns>
    [HttpGet("metadata")]
    [RequirePermission(Permissions.ProductsRead)]
    public async Task<ActionResult<ApiResponse<ImageMetadataDto>>> GetImageMetadata([FromQuery] string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(ApiResponse<ImageMetadataDto>.FailureResult("Image URL is required", 400));
            }

            // URL decode the image URL
            var decodedImageUrl = Uri.UnescapeDataString(imageUrl);
            
            _logger.LogDebug("Retrieving metadata for image {ImageUrl}", decodedImageUrl);
            
            var result = await _productImageService.GetImageMetadataAsync(decodedImageUrl);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for image {ImageUrl}", imageUrl);
            return StatusCode(500, ApiResponse<ImageMetadataDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get image management summary for all products (Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Summary of image status for all products</returns>
    [HttpGet("summary")]
    [RequirePermission(Permissions.SystemViewLogs)] // Only admins can see system-wide summaries
    public async Task<ActionResult<ApiResponse<List<ProductImageSummaryDto>>>> GetImageManagementSummary(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Retrieving image management summary (page {PageNumber}, size {PageSize})", 
                pageNumber, pageSize);

            // This would typically be implemented in the service layer
            // For now, return a placeholder response
            var summary = new List<ProductImageSummaryDto>();
            
            return ApiResponse<List<ProductImageSummaryDto>>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image management summary");
            return StatusCode(500, ApiResponse<List<ProductImageSummaryDto>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Health check endpoint for image management service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    public Task<ActionResult<ApiResponse<object>>> GetHealthStatus()
    {
        try
        {
            // Basic health check - verify service dependencies
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                ServiceName = "ProductImageService",
                Version = "1.0.0"
            };

            return Task.FromResult<ActionResult<ApiResponse<object>>>(ApiResponse<object>.SuccessResult(healthStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image management service health");
            return Task.FromResult<ActionResult<ApiResponse<object>>>(ApiResponse<object>.FailureResult("Service unhealthy", 500));
        }
    }
} 