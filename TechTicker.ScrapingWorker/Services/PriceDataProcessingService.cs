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
    private readonly IProductImageService _productImageService;

    public PriceDataProcessingService(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IOptions<MessagingConfiguration> messagingConfig,
        ILogger<PriceDataProcessingService> logger,
        IProductImageService productImageService)
    {
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
        _productImageService = productImageService;
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
            await _unitOfWork.SaveChangesAsync();

            // Update product images if provided
            await UpdateProductImagesIfProvidedAsync(normalizedData);

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

            _logger.LogInformation("Successfully processed and recorded price point for product {ProductId}: ${Price}", 
                rawData.CanonicalProductId, normalizedData.ScrapedPrice);
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
                ScrapedProductName = rawData.ScrapedProductName?.Trim()
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

            if (!hasImageData)
            {
                _logger.LogDebug("No image data provided for product {ProductId}, skipping image update",
                    normalizedData.CanonicalProductId);
                return;
            }

            _logger.LogInformation("Updating product images for product {ProductId}",
                normalizedData.CanonicalProductId);

            await _productImageService.UpdateProductImagesAsync(
                normalizedData.CanonicalProductId,
                normalizedData.PrimaryImageUrl,
                normalizedData.AdditionalImageUrls,
                normalizedData.OriginalImageUrls);

            _logger.LogInformation("Successfully updated product images for product {ProductId}",
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
