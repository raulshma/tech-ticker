using TechTicker.PriceNormalizationService.Messages;
using TechTicker.PriceNormalizationService.Models;
using TechTicker.Shared.Utilities;
using TechTicker.Shared.Common;

namespace TechTicker.PriceNormalizationService.Services
{
    /// <summary>
    /// Service for normalizing and validating raw price data
    /// </summary>
    public class PriceNormalizationService : IPriceNormalizationService
    {
        private readonly ILogger<PriceNormalizationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly NormalizationSettings _settings;

        public PriceNormalizationService(
            ILogger<PriceNormalizationService> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _settings = new NormalizationSettings();
            configuration.GetSection("NormalizationSettings").Bind(_settings);
        }

        public Task<Result<NormalizationResult>> NormalizeRawPriceDataAsync(RawPriceDataEvent rawData)
        {
            try
            {
                _logger.LogDebug("Starting normalization for product {ProductId} from {Seller}", 
                    rawData.CanonicalProductId, rawData.SellerName);

                var result = new NormalizationResult
                {
                    ProductName = string.IsNullOrWhiteSpace(rawData.ScrapedProductName) ? null : rawData.ScrapedProductName.Trim(),
                    OriginalStockStatus = rawData.ScrapedStockStatus
                };

                // Validate and normalize price
                var priceValidation = ValidateAndNormalizePrice(rawData.ScrapedPrice);                if (priceValidation.IsFailure)
                {
                    result.IsValid = false;
                    result.ValidationErrors.AddRange(priceValidation.ErrorMessage?.Split(';') ?? new[] { "Price validation failed" });
                    
                    if (_settings.StrictValidation)
                    {
                        return Task.FromResult(Result<NormalizationResult>.Failure(
                            $"Price validation failed: {priceValidation.ErrorMessage}", 
                            NormalizationErrorCodes.InvalidPrice));
                    }
                }
                else
                {
                    result.NormalizedPrice = priceValidation.Data;
                }

                // Normalize stock status
                result.NormalizedStockStatus = NormalizeStockStatus(rawData.ScrapedStockStatus);

                // Validate required data                if (!ValidateRequiredData(rawData))
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add("Missing required data fields");
                    
                    if (_settings.StrictValidation)
                    {
                        return Task.FromResult(Result<NormalizationResult>.Failure(
                            "Missing required data fields", 
                            NormalizationErrorCodes.MissingRequiredData));
                    }
                }

                result.IsValid = result.ValidationErrors.Count == 0;                _logger.LogDebug("Normalization completed for product {ProductId}: Price={Price}, Stock={Stock}, Valid={Valid}", 
                    rawData.CanonicalProductId, result.NormalizedPrice, result.NormalizedStockStatus, result.IsValid);

                return Task.FromResult(Result<NormalizationResult>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing price data for product {ProductId}", rawData.CanonicalProductId);
                return Task.FromResult(Result<NormalizationResult>.Failure(ex.Message, NormalizationErrorCodes.ValidationError));
            }
        }

        private Result<decimal> ValidateAndNormalizePrice(decimal price)
        {
            try
            {
                // Check if price is valid
                if (price <= 0)
                {
                    return Result<decimal>.Failure("Price must be greater than zero", NormalizationErrorCodes.InvalidPrice);
                }

                // Check price range
                if (price < _settings.MinPrice || price > _settings.MaxPrice)
                {
                    return Result<decimal>.Failure(
                        $"Price {price} is outside valid range [{_settings.MinPrice}, {_settings.MaxPrice}]", 
                        NormalizationErrorCodes.PriceOutOfRange);
                }

                // Round to 2 decimal places for consistency
                var normalizedPrice = Math.Round(price, 2, MidpointRounding.AwayFromZero);
                
                return Result<decimal>.Success(normalizedPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating price: {Price}", price);
                return Result<decimal>.Failure($"Price validation error: {ex.Message}", NormalizationErrorCodes.InvalidPrice);
            }
        }

        private string NormalizeStockStatus(string stockStatus)
        {
            if (string.IsNullOrWhiteSpace(stockStatus))
            {
                return _settings.DefaultStockStatus;
            }

            var lowerStock = stockStatus.ToLowerInvariant().Trim();

            // Check for in-stock indicators
            if (lowerStock.Contains("in stock") || 
                lowerStock.Contains("available") || 
                lowerStock.Contains("in-stock") || 
                lowerStock.Contains("ready") ||
                lowerStock.Contains("ships") ||
                lowerStock.Contains("delivery"))
            {
                return StockStatusConstants.InStock;
            }

            // Check for out-of-stock indicators
            if (lowerStock.Contains("out of stock") || 
                lowerStock.Contains("unavailable") ||
                lowerStock.Contains("out-of-stock") || 
                lowerStock.Contains("sold out") ||
                lowerStock.Contains("not available") ||
                lowerStock.Contains("temporarily unavailable"))
            {
                return StockStatusConstants.OutOfStock;
            }

            // Check for limited stock indicators
            if (lowerStock.Contains("limited") || 
                lowerStock.Contains("few left") ||
                lowerStock.Contains("low stock") ||
                lowerStock.Contains("only") && lowerStock.Contains("left"))
            {
                return StockStatusConstants.LimitedStock;
            }

            // Check for pre-order indicators
            if (lowerStock.Contains("pre-order") || 
                lowerStock.Contains("preorder") ||
                lowerStock.Contains("coming soon") ||
                lowerStock.Contains("pre order"))
            {
                return StockStatusConstants.PreOrder;
            }

            // Check for discontinued indicators
            if (lowerStock.Contains("discontinued") || 
                lowerStock.Contains("no longer available") ||
                lowerStock.Contains("end of life"))
            {
                return StockStatusConstants.Discontinued;
            }

            // Log unrecognized status for future improvement
            _logger.LogDebug("Unrecognized stock status: '{StockStatus}', defaulting to {DefaultStatus}", 
                stockStatus, _settings.DefaultStockStatus);

            return _settings.DefaultStockStatus;
        }

        private bool ValidateRequiredData(RawPriceDataEvent rawData)
        {
            return rawData.CanonicalProductId != Guid.Empty &&
                   !string.IsNullOrWhiteSpace(rawData.SellerName) &&
                   !string.IsNullOrWhiteSpace(rawData.SourceUrl) &&
                   rawData.Timestamp != DateTimeOffset.MinValue;
        }
    }
}
