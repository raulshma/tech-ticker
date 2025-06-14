using TechTicker.PriceNormalizationService.Messages;
using TechTicker.PriceNormalizationService.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.PriceNormalizationService.Services
{
    /// <summary>
    /// Interface for price normalization operations
    /// </summary>
    public interface IPriceNormalizationService
    {
        /// <summary>
        /// Normalizes and validates raw price data
        /// </summary>
        /// <param name="rawData">The raw price data to normalize</param>
        /// <returns>The normalization result</returns>
        Task<Result<NormalizationResult>> NormalizeRawPriceDataAsync(RawPriceDataEvent rawData);
    }

    /// <summary>
    /// Interface for consuming messages from the message broker
    /// </summary>
    public interface IMessageConsumerService
    {
        /// <summary>
        /// Starts consuming raw price data events
        /// </summary>
        Task StartConsumingAsync(Func<RawPriceDataEvent, Task> messageHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Stops consuming messages
        /// </summary>
        Task StopConsumingAsync();
    }

    /// <summary>
    /// Interface for publishing messages to the message broker
    /// </summary>
    public interface IMessagePublisherService
    {
        /// <summary>
        /// Publishes a price point recorded event
        /// </summary>
        Task PublishPricePointRecordedAsync(PricePointRecordedEvent pricePointEvent);
    }
}
