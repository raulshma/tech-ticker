using TechTicker.PriceHistoryService.Messages;
using TechTicker.PriceHistoryService.Services;

namespace TechTicker.PriceHistoryService.Workers
{
    /// <summary>
    /// Background worker that consumes price point recorded events and stores them in the price history database
    /// </summary>
    public class PriceHistoryIngestionWorker : BackgroundService
    {
        private readonly ILogger<PriceHistoryIngestionWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PriceHistoryIngestionWorker(
            ILogger<PriceHistoryIngestionWorker> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Price History Ingestion Worker starting");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumerService>();

                await messageConsumer.StartConsumingAsync(ProcessPricePointRecorded, stoppingToken);

                // Keep the worker running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken);
                }

                await messageConsumer.StopConsumingAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Price History Ingestion Worker was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Price History Ingestion Worker");
                throw;
            }

            _logger.LogInformation("Price History Ingestion Worker stopped");
        }

        private async Task ProcessPricePointRecorded(PricePointRecordedEvent pricePointEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var priceHistoryService = scope.ServiceProvider.GetRequiredService<IPriceHistoryService>();

            _logger.LogInformation("Processing price point for product {ProductId} from {Seller} at price {Price}",
                pricePointEvent.CanonicalProductId, pricePointEvent.SellerName, pricePointEvent.Price);

            try
            {
                // Store the price point in the history database
                var result = await priceHistoryService.StorePricePointAsync(pricePointEvent);

                if (result.IsFailure)
                {
                    _logger.LogError("Failed to store price point for product {ProductId}: {Error}",
                        pricePointEvent.CanonicalProductId, result.ErrorMessage);
                    return;
                }

                _logger.LogInformation("Successfully stored price point {RecordId} for product {ProductId}",
                    result.Data, pricePointEvent.CanonicalProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing price point for product {ProductId}",
                    pricePointEvent.CanonicalProductId);
                throw; // Re-throw to trigger message requeue
            }
        }
    }
}
