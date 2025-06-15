using TechTicker.Application.Messages;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for processing price alerts
/// </summary>
public interface IAlertProcessingService
{
    Task ProcessPricePointAsync(PricePointRecordedEvent pricePoint);
    Task SendAlertNotificationAsync(AlertTriggeredEvent alertEvent);
}
