using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Product Image Management operations
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Upload multiple images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="images">List of images to upload</param>
    /// <returns>Result with uploaded image information</returns>
    Task<Result<List<ImageDto>>> UploadImagesAsync(Guid productId, List<ImageUploadDto> images);

    /// <summary>
    /// Get all images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Result with list of product images</returns>
    Task<Result<List<ImageDto>>> GetProductImagesAsync(Guid productId);

    /// <summary>
    /// Delete a specific image from a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">Image URL to delete</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> DeleteImageAsync(Guid productId, string imageUrl);

    /// <summary>
    /// Set a specific image as the primary image for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">Image URL to set as primary</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> SetPrimaryImageAsync(Guid productId, string imageUrl);

    /// <summary>
    /// Reorder images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrls">List of image URLs in the desired order</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> ReorderImagesAsync(Guid productId, List<string> imageUrls);

    /// <summary>
    /// Delete multiple images from a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrls">List of image URLs to delete</param>
    /// <returns>Result with information about successful and failed deletions</returns>
    Task<Result<BulkImageOperationResultDto>> BulkDeleteImagesAsync(Guid productId, List<string> imageUrls);

    /// <summary>
    /// Get image metadata including file size and dimensions
    /// </summary>
    /// <param name="imageUrl">Image URL</param>
    /// <returns>Result with image metadata</returns>
    Task<Result<ImageMetadataDto>> GetImageMetadataAsync(string imageUrl);

    /// <summary>
    /// Validate if the current user has permission to manage images for the specified product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasImageManagementPermissionAsync(Guid productId);

    /// <summary>
    /// Get existing image mappings for a product (URL -> local path)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Dictionary mapping image URLs to local paths</returns>
    Task<Dictionary<string, string>> GetExistingImageMappingsAsync(Guid productId);

    /// <summary>
    /// Update product images with primary, additional, and original image URLs
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="primaryImageUrl">Primary image URL</param>
    /// <param name="additionalImageUrls">Additional image URLs</param>
    /// <param name="originalImageUrls">Original image URLs</param>
    /// <returns>Task</returns>
    Task UpdateProductImagesAsync(Guid productId, string? primaryImageUrl, List<string>? additionalImageUrls, List<string>? originalImageUrls);

    /// <summary>
    /// Check if a product has images updated within the specified time frame
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="maxAge">Maximum age of images to consider recent</param>
    /// <returns>True if product has recent images</returns>
    Task<bool> HasRecentImagesAsync(Guid productId, TimeSpan maxAge);

    /// <summary>
    /// Check if a product has any images
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>True if product has any images</returns>
    Task<bool> HasAnyImagesAsync(Guid productId);

    /// <summary>
    /// Check if image processing should be skipped for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="maxAge">Maximum age to consider images recent</param>
    /// <param name="maxImages">Maximum number of images needed</param>
    /// <returns>True if image processing should be skipped</returns>
    Task<bool> ShouldSkipImageProcessingAsync(Guid productId, TimeSpan maxAge, int maxImages);
}
