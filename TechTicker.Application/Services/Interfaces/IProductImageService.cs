namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing product images
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Update product images in the database
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="primaryImageUrl">Primary image URL (local path)</param>
    /// <param name="additionalImageUrls">Additional image URLs (local paths)</param>
    /// <param name="originalImageUrls">Original scraped image URLs for reference</param>
    Task UpdateProductImagesAsync(
        Guid productId,
        string? primaryImageUrl,
        List<string>? additionalImageUrls,
        List<string>? originalImageUrls);

    /// <summary>
    /// Check if product has recent images (within specified age)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="maxAge">Maximum age of images to consider recent</param>
    /// <returns>True if product has recent images</returns>
    Task<bool> HasRecentImagesAsync(Guid productId, TimeSpan maxAge);

    /// <summary>
    /// Get existing image URLs for a product to avoid re-downloading
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Dictionary mapping original URLs to local paths</returns>
    Task<Dictionary<string, string>> GetExistingImageMappingsAsync(Guid productId);

    /// <summary>
    /// Check if a product already has any images stored
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>True if product has any images</returns>
    Task<bool> HasAnyImagesAsync(Guid productId);

    /// <summary>
    /// Check if a product has recent enough images to skip re-processing
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="maxAge">Maximum age of images to consider recent</param>
    /// <param name="minImageCount">Minimum number of images required</param>
    /// <returns>True if product has recent enough images</returns>
    Task<bool> ShouldSkipImageProcessingAsync(Guid productId, TimeSpan maxAge, int minImageCount = 1);
}
