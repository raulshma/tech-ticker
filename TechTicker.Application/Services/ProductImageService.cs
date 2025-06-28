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

    public async Task<Dictionary<string, string>> GetExistingImageMappingsAsync(Guid productId)
    {
        try
        {
            var mappings = new Dictionary<string, string>();

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogDebug("Product {ProductId} not found, returning empty image mappings", productId);
                return mappings;
            }

            // Get original URLs and corresponding local paths
            var originalUrls = product.OriginalImageUrlsList ?? new List<string>();
            var localPaths = new List<string>();

            // Combine primary and additional images
            if (!string.IsNullOrEmpty(product.PrimaryImageUrl))
            {
                localPaths.Add(product.PrimaryImageUrl);
            }

            if (product.AdditionalImageUrlsList != null)
            {
                localPaths.AddRange(product.AdditionalImageUrlsList);
            }

            // Create mapping with improved logic
            CreateImageMappings(originalUrls, localPaths, mappings);

            _logger.LogDebug("Found {Count} existing image mappings for product {ProductId}",
                mappings.Count, productId);

            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting existing image mappings for product {ProductId}", productId);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Create image mappings with improved logic to handle mismatched counts
    /// </summary>
    private void CreateImageMappings(List<string> originalUrls, List<string> localPaths, Dictionary<string, string> mappings)
    {
        // If we have the same number of original URLs and local paths, map them in order
        if (originalUrls.Count == localPaths.Count)
        {
            for (int i = 0; i < originalUrls.Count; i++)
            {
                if (!string.IsNullOrEmpty(originalUrls[i]) && !string.IsNullOrEmpty(localPaths[i]))
                {
                    mappings[originalUrls[i]] = localPaths[i];
                }
            }
        }
        else if (originalUrls.Count > 0 && localPaths.Count > 0)
        {
            // If counts don't match, try to match by filename patterns
            // This is a fallback for cases where the order might be different
            foreach (var originalUrl in originalUrls.Where(u => !string.IsNullOrEmpty(u)))
            {
                var urlFileName = ExtractFileNameFromUrl(originalUrl);
                if (!string.IsNullOrEmpty(urlFileName))
                {
                    var matchingPath = localPaths.FirstOrDefault(path => 
                        !string.IsNullOrEmpty(path) && 
                        Path.GetFileName(path).Contains(urlFileName, StringComparison.OrdinalIgnoreCase));
                    
                    if (!string.IsNullOrEmpty(matchingPath))
                    {
                        mappings[originalUrl] = matchingPath;
                    }
                }
            }

            // If we still don't have mappings, just map what we can in order
            if (mappings.Count == 0)
            {
                var minCount = Math.Min(originalUrls.Count, localPaths.Count);
                for (int i = 0; i < minCount; i++)
                {
                    if (!string.IsNullOrEmpty(originalUrls[i]) && !string.IsNullOrEmpty(localPaths[i]))
                    {
                        mappings[originalUrls[i]] = localPaths[i];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extract filename from URL for matching purposes
    /// </summary>
    private string? ExtractFileNameFromUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var path = uri.LocalPath;
                var fileName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Remove query parameters and get just the filename
                    var cleanFileName = fileName.Split('?')[0].Split('#')[0];
                    return cleanFileName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting filename from URL: {Url}", url);
        }
        return null;
    }

    public async Task<bool> HasAnyImagesAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            // Check if product has any image URLs stored
            var hasPrimaryImage = !string.IsNullOrEmpty(product.PrimaryImageUrl);
            var hasAdditionalImages = product.AdditionalImageUrlsList?.Count > 0;
            var hasOriginalImages = product.OriginalImageUrlsList?.Count > 0;

            var hasImages = hasPrimaryImage || hasAdditionalImages || hasOriginalImages;

            _logger.LogDebug("Product {ProductId} has images: Primary={Primary}, Additional={Additional}, Original={Original}, HasAny={HasAny}",
                productId, hasPrimaryImage, hasAdditionalImages, hasOriginalImages, hasImages);

            return hasImages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} has any images", productId);
            return false;
        }
    }
}
