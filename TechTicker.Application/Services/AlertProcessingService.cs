using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Constants;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for processing price alerts
/// </summary>
public class AlertProcessingService : IAlertProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IAlertPerformanceMonitoringService _performanceMonitoring;
    private readonly MessagingConfiguration _messagingConfig;
    private readonly ILogger<AlertProcessingService> _logger;

    public AlertProcessingService(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IAlertPerformanceMonitoringService performanceMonitoring,
        IOptions<MessagingConfiguration> messagingConfig,
        ILogger<AlertProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _performanceMonitoring = performanceMonitoring;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
    }

    public async Task ProcessPricePointAsync(PricePointRecordedEvent pricePoint)
    {
        try
        {
            // Get active alert rules for this product
            var alertRules = await _unitOfWork.AlertRules.GetActiveAlertsForProductAsync(
                pricePoint.CanonicalProductId, pricePoint.SellerName);

            foreach (var alertRule in alertRules)
            {
                var stopwatch = Stopwatch.StartNew();
                bool wasTriggered = false;
                bool hadError = false;
                string? errorMessage = null;

                try
                {
                    // Check if enough time has passed since last notification
                    if (alertRule.LastNotifiedAt.HasValue)
                    {
                        var timeSinceLastNotification = DateTimeOffset.UtcNow - alertRule.LastNotifiedAt.Value;
                        if (timeSinceLastNotification.TotalMinutes < alertRule.NotificationFrequencyMinutes)
                        {
                            continue; // Skip this alert rule
                        }
                    }

                    bool shouldTrigger = await ShouldTriggerAlert(alertRule, pricePoint);

                    if (shouldTrigger)
                    {
                        wasTriggered = true;
                        await TriggerAlertAsync(alertRule, pricePoint);
                    }
                }
                catch (Exception ex)
                {
                    hadError = true;
                    errorMessage = ex.Message;
                    _logger.LogError(ex, "Error evaluating alert rule {AlertRuleId}", alertRule.AlertRuleId);
                }
                finally
                {
                    stopwatch.Stop();

                    // Record performance metrics
                    await _performanceMonitoring.RecordAlertEvaluationMetricsAsync(
                        alertRule.AlertRuleId,
                        stopwatch.Elapsed,
                        wasTriggered,
                        hadError,
                        errorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing price point for product {ProductId}", pricePoint.CanonicalProductId);
            throw;
        }
    }

    public async Task SendAlertNotificationAsync(AlertTriggeredEvent alertEvent)
    {
        try
        {
            // Publish to notification queue for email processing
            await _messagePublisher.PublishAsync(
                alertEvent,
                _messagingConfig.AlertsExchange,
                _messagingConfig.AlertTriggeredRoutingKey);

            _logger.LogInformation("Sent alert notification for user {UserId} and product {ProductId}",
                alertEvent.UserId, alertEvent.CanonicalProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending alert notification for alert rule {AlertRuleId}", alertEvent.AlertRuleId);
            throw;
        }
    }

    private async Task<bool> ShouldTriggerAlert(AlertRule alertRule, PricePointRecordedEvent pricePoint)
    {
        switch (alertRule.ConditionType)
        {
            case "PRICE_BELOW":
                return alertRule.ThresholdValue.HasValue && pricePoint.Price <= alertRule.ThresholdValue.Value;

            case "PERCENT_DROP_FROM_LAST":
                if (!alertRule.PercentageValue.HasValue)
                    return false;

                var lastPrice = await _unitOfWork.PriceHistory.GetLastPriceAsync(
                    pricePoint.CanonicalProductId, pricePoint.SellerName);

                if (lastPrice == null)
                    return false;

                var percentageChange = ((lastPrice.Price - pricePoint.Price) / lastPrice.Price) * 100;
                return percentageChange >= alertRule.PercentageValue.Value;

            case "BACK_IN_STOCK":
                var lastStockStatus = await _unitOfWork.PriceHistory.GetLastStockStatusAsync(
                    pricePoint.CanonicalProductId, pricePoint.SellerName);

                // Normalize both current and previous stock statuses
                var normalizedCurrentStatus = StockStatus.Normalize(pricePoint.StockStatus);
                var normalizedLastStatus = StockStatus.Normalize(lastStockStatus);

                // Use the improved stock status logic
                return StockStatus.IsBackInStock(normalizedLastStatus, normalizedCurrentStatus);

            default:
                return false;
        }
    }

    private async Task TriggerAlertAsync(AlertRule alertRule, PricePointRecordedEvent pricePoint)
    {
        try
        {
            var alertEvent = new AlertTriggeredEvent
            {
                AlertRuleId = alertRule.AlertRuleId,
                UserId = alertRule.UserId,
                UserEmail = alertRule.User?.Email ?? "unknown@example.com",
                UserFirstName = alertRule.User?.FirstName,
                CanonicalProductId = pricePoint.CanonicalProductId,
                ProductName = alertRule.Product?.Name ?? "Unknown Product",
                ProductCategoryName = alertRule.Product?.Category?.Name,
                SellerName = pricePoint.SellerName,
                TriggeringPrice = pricePoint.Price,
                TriggeringStockStatus = pricePoint.StockStatus,
                RuleDescription = alertRule.RuleDescription,
                ProductPageUrl = pricePoint.SourceUrl,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Create alert history record
            var alertHistory = new AlertHistory
            {
                AlertHistoryId = Guid.NewGuid(),
                AlertRuleId = alertRule.AlertRuleId,
                UserId = alertRule.UserId,
                CanonicalProductId = pricePoint.CanonicalProductId,
                ConditionType = alertRule.ConditionType,
                AlertType = alertRule.AlertType,
                ThresholdValue = alertRule.ThresholdValue,
                PercentageValue = alertRule.PercentageValue,
                SpecificSellerName = alertRule.SpecificSellerName,
                SellerName = pricePoint.SellerName,
                TriggeringPrice = pricePoint.Price,
                TriggeringStockStatus = pricePoint.StockStatus,
                ProductPageUrl = pricePoint.SourceUrl,
                RuleDescription = alertRule.RuleDescription,
                TriggeredAt = DateTimeOffset.UtcNow,
                NotificationStatus = "PENDING",
                WasAlertDeactivated = alertRule.AlertType == "ONE_SHOT"
            };

            await _unitOfWork.AlertHistories.AddAsync(alertHistory);

            await SendAlertNotificationAsync(alertEvent);

            // Update last notified time and handle deactivation
            alertRule.LastNotifiedAt = DateTimeOffset.UtcNow;

            // Deactivate one-shot alerts after triggering
            if (alertRule.AlertType == "ONE_SHOT")
            {
                alertRule.IsActive = false;
                _logger.LogInformation("Deactivated one-shot alert {AlertRuleId} after triggering", alertRule.AlertRuleId);
            }

            _unitOfWork.AlertRules.Update(alertRule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Triggered alert {AlertRuleId} for user {UserId}. Alert type: {AlertType}, Active: {IsActive}",
                alertRule.AlertRuleId, alertRule.UserId, alertRule.AlertType, alertRule.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering alert {AlertRuleId}", alertRule.AlertRuleId);
            throw;
        }
    }
}
