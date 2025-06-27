using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing product images
/// </summary>
public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductImageService> _logger;

    public ProductImageService(
        IUnitOfWork unitOfWork,
        ILogger<ProductImageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task UpdateProductImagesAsync(
        Guid productId,
        string? primaryImageUrl,
        List<string>? additionalImageUrls,
        List<string>? originalImageUrls)
    {
        try
        {
            _logger.LogInformation("Updating images for product {ProductId}", productId);

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found, cannot update images", productId);
                return;
            }

            // Update image URLs if provided
            if (!string.IsNullOrEmpty(primaryImageUrl))
            {
                product.PrimaryImageUrl = primaryImageUrl;
                _logger.LogDebug("Updated primary image URL for product {ProductId}: {Url}", 
                    productId, primaryImageUrl);
            }

            if (additionalImageUrls != null && additionalImageUrls.Count > 0)
            {
                product.AdditionalImageUrlsList = additionalImageUrls;
                _logger.LogDebug("Updated {Count} additional image URLs for product {ProductId}", 
                    additionalImageUrls.Count, productId);
            }

            if (originalImageUrls != null && originalImageUrls.Count > 0)
            {
                product.OriginalImageUrlsList = originalImageUrls;
                _logger.LogDebug("Updated {Count} original image URLs for product {ProductId}", 
                    originalImageUrls.Count, productId);
            }

            // Update timestamp
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            // Save changes
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully updated images for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating images for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<bool> HasRecentImagesAsync(Guid productId, TimeSpan maxAge)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product?.ImageLastUpdated == null)
            {
                return false;
            }

            var age = DateTimeOffset.UtcNow - product.ImageLastUpdated.Value;
            return age <= maxAge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image age for product {ProductId}", productId);
            return false;
        }
    }
}
