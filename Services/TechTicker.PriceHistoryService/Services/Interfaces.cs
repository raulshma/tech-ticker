using TechTicker.PriceHistoryService.Messages;
using TechTicker.PriceHistoryService.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.PriceHistoryService.Services
{
    /// <summary>
    /// Interface for price history data operations
    /// </summary>
    public interface IPriceHistoryService
    {
        /// <summary>
        /// Stores a price point record in the history database
        /// </summary>
        Task<Result<long>> StorePricePointAsync(PricePointRecordedEvent pricePoint);

        /// <summary>
        /// Retrieves price history for a product with optional filtering
        /// </summary>
        Task<Result<PriceHistoryQueryResponse>> GetPriceHistoryAsync(PriceHistoryQueryRequest request);

        /// <summary>
        /// Gets price statistics for a product/seller combination
        /// </summary>
        Task<Result<PriceStatisticsDto>> GetPriceStatisticsAsync(Guid canonicalProductId, string? sellerName = null);

        /// <summary>
        /// Gets the latest price for a product from a specific seller
        /// </summary>
        Task<Result<PriceHistoryPointDto?>> GetLatestPriceAsync(Guid canonicalProductId, string sellerName);

        /// <summary>
        /// Gets all sellers that have price data for a specific product
        /// </summary>
        Task<Result<List<string>>> GetSellersForProductAsync(Guid canonicalProductId);
    }

    /// <summary>
    /// Interface for consuming price point recorded events from message broker
    /// </summary>
    public interface IMessageConsumerService
    {
        /// <summary>
        /// Starts consuming price point recorded events
        /// </summary>
        Task StartConsumingAsync(Func<PricePointRecordedEvent, Task> messageHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Stops consuming messages
        /// </summary>
        Task StopConsumingAsync();
    }
}
