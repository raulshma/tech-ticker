using TechTicker.ScraperService.Messages;
using TechTicker.ScraperService.Models;
using TechTicker.Shared.Utilities;
using TechTicker.Shared.Common;

namespace TechTicker.ScraperService.Services
{
    /// <summary>
    /// Interface for web scraping operations
    /// </summary>
    public interface IWebScrapingService
    {
        /// <summary>
        /// Scrapes a product page using the provided command
        /// </summary>
        /// <param name="command">The scraping command with URL and selectors</param>
        /// <returns>The scraping result</returns>
        Task<Result<ScrapingResult>> ScrapeProductPageAsync(ScrapeProductPageCommand command);
    }

    /// <summary>
    /// Interface for HTML parsing operations
    /// </summary>
    public interface IHtmlParsingService
    {
        /// <summary>
        /// Extracts product data from HTML content using provided selectors
        /// </summary>
        /// <param name="html">The HTML content to parse</param>
        /// <param name="selectors">The selectors to use for extraction</param>
        /// <returns>The extracted product data</returns>
        Result<(string? productName, decimal? price, string? stockStatus)> ExtractProductData(
            string html, ScrapingSelectors selectors);
    }

    /// <summary>
    /// Interface for publishing messages to the message broker
    /// </summary>
    public interface IMessagePublisherService
    {
        /// <summary>
        /// Publishes a raw price data event
        /// </summary>
        Task PublishRawPriceDataAsync(RawPriceDataEvent priceData);

        /// <summary>
        /// Publishes a scraping result event
        /// </summary>
        Task PublishScrapingResultAsync(ScrapingResultEvent result);
    }

    /// <summary>
    /// Interface for consuming messages from the message broker
    /// </summary>
    public interface IMessageConsumerService
    {
        /// <summary>
        /// Starts consuming scrape product page commands
        /// </summary>
        Task StartConsumingAsync(Func<ScrapeProductPageCommand, Task> messageHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Stops consuming messages
        /// </summary>
        Task StopConsumingAsync();
    }
}
