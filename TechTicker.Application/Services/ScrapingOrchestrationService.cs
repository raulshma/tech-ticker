using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for orchestrating scraping operations
/// </summary>
public class ScrapingOrchestrationService : IScrapingOrchestrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly MessagingConfiguration _messagingConfig;
    private readonly ILogger<ScrapingOrchestrationService> _logger;

    public ScrapingOrchestrationService(
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IOptions<MessagingConfiguration> messagingConfig,
        ILogger<ScrapingOrchestrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _messagingConfig = messagingConfig.Value;
        _logger = logger;
    }

    public async Task ScheduleScrapingJobsAsync()
    {
        try
        {
            var mappingsDueForScraping = await _unitOfWork.ProductSellerMappings.GetMappingsDueForScrapingAsync(100);

            foreach (var mapping in mappingsDueForScraping)
            {
                var command = new ScrapeProductPageCommand
                {
                    MappingId = mapping.MappingId,
                    CanonicalProductId = mapping.CanonicalProductId,
                    SellerName = mapping.SellerName,
                    ExactProductUrl = mapping.ExactProductUrl,
                    Selectors = new ScrapingSelectors
                    {
                        ProductNameSelector = mapping.SiteConfiguration?.ProductNameSelector ?? "h1",
                        PriceSelector = mapping.SiteConfiguration?.PriceSelector ?? ".price",
                        StockSelector = mapping.SiteConfiguration?.StockSelector ?? ".stock",
                        SellerNameOnPageSelector = mapping.SiteConfiguration?.SellerNameOnPageSelector
                    },
                    ScrapingProfile = new ScrapingProfile
                    {
                        UserAgent = mapping.SiteConfiguration?.DefaultUserAgent ?? 
                                   "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
                        Headers = mapping.SiteConfiguration?.AdditionalHeadersDict
                    }
                };

                await _messagePublisher.PublishAsync(
                    command,
                    _messagingConfig.ScrapingExchange,
                    _messagingConfig.ScrapeCommandRoutingKey);

                // Update next scrape time
                mapping.NextScrapeAt = CalculateNextScrapeTime(mapping.ScrapingFrequencyOverride);
                _unitOfWork.ProductSellerMappings.Update(mapping);

                _logger.LogDebug("Scheduled scraping job for mapping {MappingId}", mapping.MappingId);
            }

            if (mappingsDueForScraping.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Scheduled {Count} scraping jobs", mappingsDueForScraping.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling scraping jobs");
            throw;
        }
    }

    public async Task ProcessScrapingResultAsync(Guid mappingId, bool wasSuccessful, string? errorMessage = null)
    {
        try
        {
            var mapping = await _unitOfWork.ProductSellerMappings.GetByIdAsync(mappingId);
            if (mapping == null)
            {
                _logger.LogWarning("Mapping {MappingId} not found for scraping result processing", mappingId);
                return;
            }

            mapping.LastScrapedAt = DateTimeOffset.UtcNow;

            if (wasSuccessful)
            {
                mapping.LastScrapeStatus = "SUCCESS";
                mapping.LastScrapeErrorCode = null;
                mapping.ConsecutiveFailureCount = 0;
            }
            else
            {
                mapping.LastScrapeStatus = "FAILED";
                mapping.LastScrapeErrorCode = errorMessage;
                mapping.ConsecutiveFailureCount++;

                // Disable mapping if too many consecutive failures
                if (mapping.ConsecutiveFailureCount >= 5)
                {
                    mapping.IsActiveForScraping = false;
                    _logger.LogWarning("Disabled mapping {MappingId} due to {FailureCount} consecutive failures",
                        mappingId, mapping.ConsecutiveFailureCount);
                }
            }

            // Calculate next scrape time
            mapping.NextScrapeAt = CalculateNextScrapeTime(mapping.ScrapingFrequencyOverride);

            _unitOfWork.ProductSellerMappings.Update(mapping);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug("Processed scraping result for mapping {MappingId}: {Status}",
                mappingId, wasSuccessful ? "SUCCESS" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scraping result for mapping {MappingId}", mappingId);
            throw;
        }
    }

    private static DateTimeOffset CalculateNextScrapeTime(string? frequencyOverride)
    {
        // Default to 1 hour if no override specified
        var defaultInterval = TimeSpan.FromHours(1);

        if (string.IsNullOrWhiteSpace(frequencyOverride))
        {
            return DateTimeOffset.UtcNow.Add(defaultInterval);
        }

        try
        {
            // Parse ISO 8601 duration format (e.g., "PT1H" for 1 hour, "PT30M" for 30 minutes)
            var interval = System.Xml.XmlConvert.ToTimeSpan(frequencyOverride);
            return DateTimeOffset.UtcNow.Add(interval);
        }
        catch
        {
            // Fall back to default if parsing fails
            return DateTimeOffset.UtcNow.Add(defaultInterval);
        }
    }
}
