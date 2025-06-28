using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Background service that consumes PricePointRecordedEvent messages for alert evaluation
/// </summary>
public class PricePointConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PricePointConsumerService> _logger;
    private readonly MessagingConfiguration _messagingConfig;
    private IMessageConsumer? _messageConsumer;

    public PricePointConsumerService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PricePointConsumerService> logger,
        IOptions<MessagingConfiguration> messagingConfig)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _messagingConfig = messagingConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PricePoint Consumer Service started");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            _messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();

            // Start consuming price point recorded events for alert evaluation
            await _messageConsumer.StartConsumingAsync<PricePointRecordedEvent>(
                _messagingConfig.PricePointRecordedQueue,
                HandlePricePointRecordedAsync);

            _logger.LogInformation("Started consuming price point messages for alert evaluation");

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PricePoint consumer service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in PricePoint consumer service");
            throw;
        }
        finally
        {
            if (_messageConsumer != null)
            {
                await _messageConsumer.StopConsumingAsync();
            }
            _logger.LogInformation("PricePoint Consumer Service stopped");
        }
    }

    private async Task HandlePricePointRecordedAsync(PricePointRecordedEvent pricePoint)
    {
        try
        {
            _logger.LogDebug("Processing price point for alert evaluation: Product {ProductId}, Seller {SellerName}, Price ${Price}",
                pricePoint.CanonicalProductId, pricePoint.SellerName, pricePoint.Price);

            using var scope = _serviceScopeFactory.CreateScope();
            var alertProcessingService = scope.ServiceProvider.GetRequiredService<IAlertProcessingService>();
            await alertProcessingService.ProcessPricePointAsync(pricePoint);

            _logger.LogDebug("Successfully processed price point for alert evaluation: Product {ProductId}",
                pricePoint.CanonicalProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing price point for alert evaluation: Product {ProductId}",
                pricePoint.CanonicalProductId);
            // Don't rethrow to prevent message requeue - log and continue
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PricePoint Consumer Service is stopping");
        
        if (_messageConsumer != null)
        {
            await _messageConsumer.StopConsumingAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
