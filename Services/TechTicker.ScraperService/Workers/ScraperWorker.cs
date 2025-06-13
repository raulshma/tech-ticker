using TechTicker.ScraperService.Messages;
using TechTicker.ScraperService.Services;

namespace TechTicker.ScraperService.Workers
{
    /// <summary>
    /// Background worker that consumes scraping commands and processes them
    /// </summary>
    public class ScraperWorker : BackgroundService
    {
        private readonly ILogger<ScraperWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ScraperWorker(ILogger<ScraperWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraper Worker starting");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumerService>();

                await messageConsumer.StartConsumingAsync(ProcessScrapeCommand, stoppingToken);

                // Keep the worker running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken);
                }

                await messageConsumer.StopConsumingAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scraper Worker was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Scraper Worker");
                throw;
            }

            _logger.LogInformation("Scraper Worker stopped");
        }

        private async Task ProcessScrapeCommand(ScrapeProductPageCommand command)
        {
            using var scope = _serviceProvider.CreateScope();
            var webScrapingService = scope.ServiceProvider.GetRequiredService<IWebScrapingService>();
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisherService>();

            _logger.LogInformation("Processing scrape command for mapping {MappingId}, URL: {Url}", 
                command.MappingId, command.ExactProductUrl);

            try
            {
                // Perform the scraping
                var scrapingResult = await webScrapingService.ScrapeProductPageAsync(command);
                
                if (scrapingResult.IsFailure)
                {
                    _logger.LogError("Failed to process scraping command: {Error}", scrapingResult.ErrorMessage);
                    
                    // Publish failure result
                    await PublishScrapingResult(command, false, scrapingResult.ErrorMessage, scrapingResult.ErrorCode, null, messagePublisher);
                    return;
                }

                var result = scrapingResult.Data!;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully scraped product {ProductName} with price {Price}", 
                        result.ProductName, result.Price);

                    // Publish raw price data
                    var priceData = new RawPriceDataEvent
                    {
                        CanonicalProductId = command.CanonicalProductId,
                        SellerName = command.SellerName,
                        ScrapedPrice = result.Price!.Value,
                        ScrapedStockStatus = result.StockStatus!,
                        Timestamp = DateTimeOffset.UtcNow,
                        SourceUrl = command.ExactProductUrl,
                        ScrapedProductName = result.ProductName ?? "Unknown"
                    };

                    await messagePublisher.PublishRawPriceDataAsync(priceData);

                    // Publish success result
                    await PublishScrapingResult(command, true, null, null, null, messagePublisher);
                }
                else
                {
                    _logger.LogWarning("Scraping failed for mapping {MappingId}: {Error} ({ErrorCode})", 
                        command.MappingId, result.ErrorMessage, result.ErrorCode);

                    // Publish failure result
                    await PublishScrapingResult(command, false, result.ErrorMessage, result.ErrorCode, result.HttpStatusCode, messagePublisher);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing scrape command for mapping {MappingId}", command.MappingId);
                
                // Publish failure result
                await PublishScrapingResult(command, false, ex.Message, "UNKNOWN_ERROR", null, messagePublisher);
            }
        }

        private async Task PublishScrapingResult(
            ScrapeProductPageCommand command, 
            bool wasSuccessful, 
            string? errorMessage, 
            string? errorCode, 
            int? httpStatusCode,
            IMessagePublisherService messagePublisher)
        {
            try
            {
                var resultEvent = new ScrapingResultEvent
                {
                    MappingId = command.MappingId,
                    WasSuccessful = wasSuccessful,
                    Timestamp = DateTimeOffset.UtcNow,
                    ErrorMessage = errorMessage,
                    ErrorCode = errorCode,
                    HttpStatusCode = httpStatusCode
                };

                await messagePublisher.PublishScrapingResultAsync(resultEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish scraping result for mapping {MappingId}", command.MappingId);
            }
        }
    }
}
