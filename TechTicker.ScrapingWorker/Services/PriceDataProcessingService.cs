using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for processing and normalizing scraped price data
/// </summary>
public class PriceDataProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly MessagingConfiguration _messagingConfig;
    private readonly ILogger<PriceDataProcessingService> _logger;

    public PriceDataProcessingService(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IOptions<MessagingConfiguration> messagingConfig,
        ILogger<PriceDataProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
    }

    public async Task ProcessRawPriceDataAsync(RawPriceDataEvent rawData)
    {
        try
        {
            _logger.LogInformation("Processing raw price data for product {ProductId} from seller {SellerName}", 
                rawData.CanonicalProductId, rawData.SellerName);

            // Validate and normalize the data
            var normalizedData = await NormalizePriceDataAsync(rawData);
            if (normalizedData == null)
            {
                _logger.LogWarning("Failed to normalize price data for product {ProductId}", rawData.CanonicalProductId);
                return;
            }

            // Check if this is a duplicate or very recent price point
            var isDuplicate = await IsDuplicatePricePointAsync(normalizedData);
            if (isDuplicate)
            {
                _logger.LogDebug("Skipping duplicate price point for product {ProductId} from seller {SellerName}", 
                    rawData.CanonicalProductId, rawData.SellerName);
                return;
            }

            // Save to price history
            var priceHistory = new PriceHistory
            {
                PriceHistoryId = Guid.NewGuid(),
                Timestamp = normalizedData.Timestamp,
                CanonicalProductId = normalizedData.CanonicalProductId,
                MappingId = normalizedData.MappingId,
                SellerName = normalizedData.SellerName,
                Price = normalizedData.ScrapedPrice,
                StockStatus = normalizedData.ScrapedStockStatus,
                SourceUrl = normalizedData.SourceUrl,
                ScrapedProductNameOnPage = normalizedData.ScrapedProductName
            };

            await _unitOfWork.PriceHistory.AddAsync(priceHistory);

            // Update product images if provided (before saving price history)
            await UpdateProductImagesIfProvidedAsync(normalizedData);

            // Save both price history and image updates in the same transaction
            await _unitOfWork.SaveChangesAsync();

            // Publish price point recorded event for alert processing
            var pricePointEvent = new PricePointRecordedEvent
            {
                CanonicalProductId = normalizedData.CanonicalProductId,
                SellerName = normalizedData.SellerName,
                Price = normalizedData.ScrapedPrice,
                StockStatus = normalizedData.ScrapedStockStatus,
                SourceUrl = normalizedData.SourceUrl,
                Timestamp = normalizedData.Timestamp
            };

            await _messagePublisher.PublishAsync(
                pricePointEvent,
                _messagingConfig.PriceDataExchange,
                _messagingConfig.PricePointRecordedRoutingKey);

            _logger.LogInformation("Successfully processed and recorded price point for product {ProductId}: ${Price}. Images: Primary={PrimaryImage}, Additional={AdditionalCount}, Original={OriginalCount}",
                rawData.CanonicalProductId, normalizedData.ScrapedPrice,
                normalizedData.PrimaryImageUrl ?? "None",
                normalizedData.AdditionalImageUrls?.Count ?? 0,
                normalizedData.OriginalImageUrls?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw price data for product {ProductId}", rawData.CanonicalProductId);
            throw;
        }
    }

    private async Task<RawPriceDataEvent?> NormalizePriceDataAsync(RawPriceDataEvent rawData)
    {
        try
        {
            // Validate required fields
            if (rawData.ScrapedPrice <= 0)
            {
                _logger.LogWarning("Invalid price {Price} for product {ProductId}", rawData.ScrapedPrice, rawData.CanonicalProductId);
                return null;
            }

            if (string.IsNullOrWhiteSpace(rawData.SellerName))
            {
                _logger.LogWarning("Missing seller name for product {ProductId}", rawData.CanonicalProductId);
                return null;
            }

            // Normalize stock status
            var normalizedStockStatus = NormalizeStockStatus(rawData.ScrapedStockStatus);

            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == rawData.CanonicalProductId);
            if (!productExists)
            {
                _logger.LogWarning("Product {ProductId} not found in database", rawData.CanonicalProductId);
                return null;
            }

            return new RawPriceDataEvent
            {
                MappingId = rawData.MappingId,
                CanonicalProductId = rawData.CanonicalProductId,
                SellerName = rawData.SellerName.Trim(),
                ScrapedPrice = Math.Round(rawData.ScrapedPrice, 2),
                ScrapedStockStatus = normalizedStockStatus,
                Timestamp = rawData.Timestamp,
                SourceUrl = rawData.SourceUrl,
                ScrapedProductName = rawData.ScrapedProductName?.Trim(),
                // Preserve image data
                PrimaryImageUrl = rawData.PrimaryImageUrl,
                AdditionalImageUrls = rawData.AdditionalImageUrls,
                OriginalImageUrls = rawData.OriginalImageUrls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing price data for product {ProductId}", rawData.CanonicalProductId);
            return null;
        }
    }

    private string NormalizeStockStatus(string stockStatus)
    {
        if (string.IsNullOrWhiteSpace(stockStatus))
            return "Unknown";

        var lowerStatus = stockStatus.ToLower().Trim();

        return lowerStatus switch
        {
            var s when s.Contains("in stock") || s.Contains("available") || s.Contains("in-stock") => "In Stock",
            var s when s.Contains("out of stock") || s.Contains("unavailable") || s.Contains("sold out") => "Out of Stock",
            var s when s.Contains("limited") || s.Contains("few left") || s.Contains("low stock") => "Limited Stock",
            var s when s.Contains("pre-order") || s.Contains("preorder") => "Pre-Order",
            var s when s.Contains("backorder") || s.Contains("back order") => "Backorder",
            _ => stockStatus.Trim()
        };
    }

    private async Task<bool> IsDuplicatePricePointAsync(RawPriceDataEvent normalizedData)
    {
        try
        {
            // Check for recent price points (within last 10 minutes) with same price and stock status
            var recentCutoff = normalizedData.Timestamp.AddMinutes(-10);
            
            var recentPriceHistory = await _unitOfWork.PriceHistory.FindAsync(ph =>
                ph.CanonicalProductId == normalizedData.CanonicalProductId &&
                ph.SellerName == normalizedData.SellerName &&
                ph.Timestamp >= recentCutoff &&
                ph.Price == normalizedData.ScrapedPrice &&
                ph.StockStatus == normalizedData.ScrapedStockStatus);

            return recentPriceHistory.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate price points for product {ProductId}", normalizedData.CanonicalProductId);
            return false; // If we can't check, allow the price point to be recorded
        }
    }

    private async Task UpdateProductImagesIfProvidedAsync(RawPriceDataEvent normalizedData)
    {
        try
        {
            // Check if any image data is provided
            var hasImageData = !string.IsNullOrEmpty(normalizedData.PrimaryImageUrl) ||
                              (normalizedData.AdditionalImageUrls?.Count > 0) ||
                              (normalizedData.OriginalImageUrls?.Count > 0);

            _logger.LogDebug("Image data check for product {ProductId}: Primary={Primary}, Additional={Additional}, Original={Original}, HasData={HasData}",
                normalizedData.CanonicalProductId,
                normalizedData.PrimaryImageUrl ?? "null",
                normalizedData.AdditionalImageUrls?.Count ?? 0,
                normalizedData.OriginalImageUrls?.Count ?? 0,
                hasImageData);

            if (!hasImageData)
            {
                _logger.LogDebug("No image data provided for product {ProductId}, skipping image update",
                    normalizedData.CanonicalProductId);
                return;
            }

            _logger.LogInformation("Updating product images for product {ProductId}",
                normalizedData.CanonicalProductId);

            // Update images directly using the same UnitOfWork to ensure same transaction
            var product = await _unitOfWork.Products.GetByIdAsync(normalizedData.CanonicalProductId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found, cannot update images",
                    normalizedData.CanonicalProductId);
                return;
            }

            // Update image URLs if provided
            if (!string.IsNullOrEmpty(normalizedData.PrimaryImageUrl))
            {
                product.PrimaryImageUrl = normalizedData.PrimaryImageUrl;
                _logger.LogDebug("Updated primary image URL for product {ProductId}: {Url}",
                    normalizedData.CanonicalProductId, normalizedData.PrimaryImageUrl);
            }

            if (normalizedData.AdditionalImageUrls != null && normalizedData.AdditionalImageUrls.Count > 0)
            {
                product.AdditionalImageUrlsList = normalizedData.AdditionalImageUrls;
                _logger.LogDebug("Updated {Count} additional image URLs for product {ProductId}",
                    normalizedData.AdditionalImageUrls.Count, normalizedData.CanonicalProductId);
            }

            if (normalizedData.OriginalImageUrls != null && normalizedData.OriginalImageUrls.Count > 0)
            {
                product.OriginalImageUrlsList = normalizedData.OriginalImageUrls;
                _logger.LogDebug("Updated {Count} original image URLs for product {ProductId}",
                    normalizedData.OriginalImageUrls.Count, normalizedData.CanonicalProductId);
            }

            // Update timestamp
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            // Update the product (will be saved with the same SaveChangesAsync call)
            _unitOfWork.Products.Update(product);

            _logger.LogInformation("Successfully prepared product images update for product {ProductId}",
                normalizedData.CanonicalProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product images for product {ProductId}",
                normalizedData.CanonicalProductId);
            // Don't rethrow - image update failure shouldn't fail the entire price processing
        }
    }
}
