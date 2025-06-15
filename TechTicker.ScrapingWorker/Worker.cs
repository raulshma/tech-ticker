using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageConsumer _messageConsumer;
    private readonly IMessagePublisher _messagePublisher;
    private readonly WebScrapingService _webScrapingService;
    private readonly PriceDataProcessingService _priceDataProcessingService;
    private readonly IScrapingOrchestrationService _orchestrationService;
    private readonly MessagingConfiguration _messagingConfig;

    public Worker(
        ILogger<Worker> logger,
        IMessageConsumer messageConsumer,
        IMessagePublisher messagePublisher,
        WebScrapingService webScrapingService,
        PriceDataProcessingService priceDataProcessingService,
        IScrapingOrchestrationService orchestrationService,
        IOptions<MessagingConfiguration> messagingConfig)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
        _messagePublisher = messagePublisher;
        _webScrapingService = webScrapingService;
        _priceDataProcessingService = priceDataProcessingService;
        _orchestrationService = orchestrationService;
        _messagingConfig = messagingConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TechTicker Scraping Worker started");

        try
        {
            // Start consuming scrape commands
            await _messageConsumer.StartConsumingAsync<ScrapeProductPageCommand>(
                _messagingConfig.ScrapeCommandQueue,
                HandleScrapeCommandAsync);

            // Start consuming raw price data for processing
            await _messageConsumer.StartConsumingAsync<RawPriceDataEvent>(
                _messagingConfig.RawPriceDataQueue,
                HandleRawPriceDataAsync);

            // Start periodic orchestration (every 5 minutes)
            var orchestrationTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await orchestrationTimer.WaitForNextTickAsync(stoppingToken);
                    await _orchestrationService.ScheduleScrapingJobsAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in orchestration cycle");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in scraping worker");
            throw;
        }
        finally
        {
            await _messageConsumer.StopConsumingAsync();
            _logger.LogInformation("TechTicker Scraping Worker stopped");
        }
    }

    private async Task HandleScrapeCommandAsync(ScrapeProductPageCommand command)
    {
        try
        {
            _logger.LogInformation("Processing scrape command for mapping {MappingId}", command.MappingId);

            // Perform the scraping
            var scrapingResult = await _webScrapingService.ScrapeProductPageAsync(command);

            // Publish scraping result
            var resultEvent = new ScrapingResultEvent
            {
                MappingId = command.MappingId,
                WasSuccessful = scrapingResult.IsSuccess,
                Timestamp = DateTimeOffset.UtcNow,
                ErrorMessage = scrapingResult.ErrorMessage,
                ErrorCode = scrapingResult.ErrorCode
            };

            await _messagePublisher.PublishAsync(
                resultEvent,
                _messagingConfig.ScrapingExchange,
                _messagingConfig.ScrapingResultRoutingKey);

            // If scraping was successful, publish raw price data
            if (scrapingResult.IsSuccess)
            {
                var rawPriceData = new RawPriceDataEvent
                {
                    CanonicalProductId = command.CanonicalProductId,
                    SellerName = command.SellerName,
                    ScrapedPrice = scrapingResult.Price,
                    ScrapedStockStatus = scrapingResult.StockStatus ?? "Unknown",
                    Timestamp = scrapingResult.ScrapedAt,
                    SourceUrl = command.ExactProductUrl,
                    ScrapedProductName = scrapingResult.ProductName
                };

                await _messagePublisher.PublishAsync(
                    rawPriceData,
                    _messagingConfig.PriceDataExchange,
                    _messagingConfig.RawPriceDataRoutingKey);
            }

            // Update mapping status
            await _orchestrationService.ProcessScrapingResultAsync(
                command.MappingId,
                scrapingResult.IsSuccess,
                scrapingResult.ErrorMessage);

            _logger.LogInformation("Completed scrape command for mapping {MappingId}: {Status}",
                command.MappingId, scrapingResult.IsSuccess ? "SUCCESS" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling scrape command for mapping {MappingId}", command.MappingId);

            // Publish failure result
            var failureEvent = new ScrapingResultEvent
            {
                MappingId = command.MappingId,
                WasSuccessful = false,
                Timestamp = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message,
                ErrorCode = "HANDLER_EXCEPTION"
            };

            await _messagePublisher.PublishAsync(
                failureEvent,
                _messagingConfig.ScrapingExchange,
                _messagingConfig.ScrapingResultRoutingKey);

            await _orchestrationService.ProcessScrapingResultAsync(command.MappingId, false, ex.Message);
        }
    }

    private async Task HandleRawPriceDataAsync(RawPriceDataEvent rawPriceData)
    {
        try
        {
            await _priceDataProcessingService.ProcessRawPriceDataAsync(rawPriceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling raw price data for product {ProductId}", rawPriceData.CanonicalProductId);
        }
    }
}
