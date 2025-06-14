using TechTicker.PriceNormalizationService.Messages;
using TechTicker.PriceNormalizationService.Services;

namespace TechTicker.PriceNormalizationService.Workers
{
    /// <summary>
    /// Background worker that consumes raw price data and processes it through normalization
    /// </summary>
    public class PriceNormalizationWorker : BackgroundService
    {
        private readonly ILogger<PriceNormalizationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PriceNormalizationWorker(ILogger<PriceNormalizationWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price Normalization Worker starting");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumerService>();

                await messageConsumer.StartConsumingAsync(ProcessRawPriceData, stoppingToken);

                // Keep the worker running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken);
                }

                await messageConsumer.StopConsumingAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Price Normalization Worker was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Price Normalization Worker");
                throw;
            }

            _logger.LogInformation("Price Normalization Worker stopped");
        }

        private async Task ProcessRawPriceData(RawPriceDataEvent rawData)
        {
            using var scope = _serviceProvider.CreateScope();
            var normalizationService = scope.ServiceProvider.GetRequiredService<IPriceNormalizationService>();
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisherService>();

            _logger.LogInformation("Processing raw price data for product {ProductId} from {Seller} at price {Price}", 
                rawData.CanonicalProductId, rawData.SellerName, rawData.ScrapedPrice);

            try
            {
                // Normalize the raw price data
                var normalizationResult = await normalizationService.NormalizeRawPriceDataAsync(rawData);
                
                if (normalizationResult.IsFailure)
                {
                    _logger.LogError("Failed to normalize price data for product {ProductId}: {Error}", 
                        rawData.CanonicalProductId, normalizationResult.ErrorMessage);
                    return;
                }

                var result = normalizationResult.Data!;

                if (!result.IsValid)
                {
                    _logger.LogWarning("Price data validation failed for product {ProductId}: {ValidationErrors}", 
                        rawData.CanonicalProductId, string.Join(", ", result.ValidationErrors));
                    
                    // Depending on business rules, we might still want to publish with warnings
                    // For now, we'll skip invalid data
                    return;
                }

                // Create the normalized price point event
                var pricePointEvent = new PricePointRecordedEvent
                {
                    CanonicalProductId = rawData.CanonicalProductId,
                    SellerName = rawData.SellerName,
                    Price = result.NormalizedPrice,
                    StockStatus = result.NormalizedStockStatus,
                    SourceUrl = rawData.SourceUrl,
                    Timestamp = rawData.Timestamp,
                    ProductName = result.ProductName,
                    OriginalStockStatus = result.OriginalStockStatus
                };

                // Publish the normalized price point
                await messagePublisher.PublishPricePointRecordedAsync(pricePointEvent);

                _logger.LogInformation("Successfully normalized and published price point for product {ProductId}: " +
                    "Price={Price}, Stock={Stock}, OriginalStock={OriginalStock}", 
                    rawData.CanonicalProductId, 
                    result.NormalizedPrice, 
                    result.NormalizedStockStatus,
                    result.OriginalStockStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing raw price data for product {ProductId}", 
                    rawData.CanonicalProductId);
            }
        }
    }
}
