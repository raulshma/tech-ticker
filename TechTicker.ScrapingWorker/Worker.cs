using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.MessageHandlers;
using TechTicker.Application.Services.Interfaces;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageConsumer _messageConsumer;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MessagingConfiguration _messagingConfig;

    public Worker(
        ILogger<Worker> logger,
        IMessageConsumer messageConsumer,
        IMessagePublisher messagePublisher,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MessagingConfiguration> messagingConfig)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
        _messagePublisher = messagePublisher;
        _serviceScopeFactory = serviceScopeFactory;
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

            // Start consuming product discovery events
            await _messageConsumer.StartConsumingAsync<ProductDiscoveryEvent>(
                _messagingConfig.ProductDiscoveryEventQueue,
                HandleProductDiscoveryEventAsync);

            // Start periodic orchestration (every 5 minutes)
            var orchestrationTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (!stoppingToken.IsCancellationRequested)            {
                try
                {
                    await orchestrationTimer.WaitForNextTickAsync(stoppingToken);
                    using var scope = _serviceScopeFactory.CreateScope();
                    var orchestrationService = scope.ServiceProvider.GetRequiredService<IScrapingOrchestrationService>();
                    await orchestrationService.ScheduleScrapingJobsAsync();
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
    }    private async Task HandleScrapeCommandAsync(ScrapeProductPageCommand command)
    {
        try
        {
            _logger.LogInformation("Processing scrape command for mapping {MappingId}", command.MappingId);

            // Create scope for scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var webScrapingService = scope.ServiceProvider.GetRequiredService<WebScrapingService>();
            var orchestrationService = scope.ServiceProvider.GetRequiredService<IScrapingOrchestrationService>();

            // Perform the scraping
            var scrapingResult = await webScrapingService.ScrapeProductPageAsync(command);

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
            await orchestrationService.ProcessScrapingResultAsync(
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

            using var scope = _serviceScopeFactory.CreateScope();
            var orchestrationService = scope.ServiceProvider.GetRequiredService<IScrapingOrchestrationService>();
            await orchestrationService.ProcessScrapingResultAsync(command.MappingId, false, ex.Message);
        }
    }

    private async Task HandleRawPriceDataAsync(RawPriceDataEvent rawPriceData)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var priceDataProcessingService = scope.ServiceProvider.GetRequiredService<PriceDataProcessingService>();
            await priceDataProcessingService.ProcessRawPriceDataAsync(rawPriceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling raw price data for product {ProductId}", rawPriceData.CanonicalProductId);
        }
    }

    private async Task HandleProductDiscoveryEventAsync(ProductDiscoveryEvent discoveryEvent)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var eventHandler = scope.ServiceProvider.GetRequiredService<ProductDiscoveryEventHandler>();
            await eventHandler.HandleAsync(discoveryEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling product discovery event {EventId}", discoveryEvent.EventId);
        }
    }
}
