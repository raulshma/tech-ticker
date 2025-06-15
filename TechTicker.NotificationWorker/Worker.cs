using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.NotificationWorker.Services;

namespace TechTicker.NotificationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageConsumer _messageConsumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MessagingConfiguration _messagingConfig;

    public Worker(
        ILogger<Worker> logger,
        IMessageConsumer messageConsumer,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MessagingConfiguration> messagingConfig)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
        _serviceScopeFactory = serviceScopeFactory;
        _messagingConfig = messagingConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TechTicker Notification Worker started");

        try
        {
            // Start consuming price point recorded events for alert processing
            await _messageConsumer.StartConsumingAsync<PricePointRecordedEvent>(
                _messagingConfig.PricePointRecordedQueue,
                HandlePricePointRecordedAsync);

            // Start consuming alert triggered events for email notifications
            await _messageConsumer.StartConsumingAsync<AlertTriggeredEvent>(
                _messagingConfig.AlertTriggeredQueue,
                HandleAlertTriggeredAsync);

            _logger.LogInformation("Started consuming notification messages");

            // Keep the worker running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Notification worker cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in notification worker");
            throw;
        }
        finally
        {
            await _messageConsumer.StopConsumingAsync();
            _logger.LogInformation("TechTicker Notification Worker stopped");
        }
    }    private async Task HandlePricePointRecordedAsync(PricePointRecordedEvent pricePoint)
    {
        try
        {
            _logger.LogDebug("Processing price point for alert evaluation: Product {ProductId}, Price ${Price}",
                pricePoint.CanonicalProductId, pricePoint.Price);

            using var scope = _serviceScopeFactory.CreateScope();
            var alertProcessingService = scope.ServiceProvider.GetRequiredService<IAlertProcessingService>();
            await alertProcessingService.ProcessPricePointAsync(pricePoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing price point for product {ProductId}", pricePoint.CanonicalProductId);
        }
    }

    private async Task HandleAlertTriggeredAsync(AlertTriggeredEvent alertEvent)
    {
        try
        {
            _logger.LogInformation("Processing alert notification for user {UserId}, product {ProductName}",
                alertEvent.UserId, alertEvent.ProductName);

            using var scope = _serviceScopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            await emailService.SendAlertNotificationAsync(alertEvent);

            _logger.LogInformation("Successfully sent alert notification for alert rule {AlertRuleId}",
                alertEvent.AlertRuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending alert notification for alert rule {AlertRuleId}", alertEvent.AlertRuleId);
        }
    }
}
