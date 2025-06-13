using Microsoft.EntityFrameworkCore;
using TechTicker.ScrapingOrchestrationService.Data;
using TechTicker.Shared.Models;

namespace TechTicker.ScrapingOrchestrationService.Services
{
    public interface IScrapingSchedulerService
    {
        Task<List<ProductSellerMapping>> GetMappingsDueForScrapingAsync(int maxMappings = 100);
        Task UpdateMappingScheduleAsync(Guid mappingId, DateTimeOffset? lastScrapedAt, DateTimeOffset? nextScrapeAt);
        Task<TimeSpan> GetScrapingFrequencyAsync(ProductSellerMapping mapping);
    }

    public class ScrapingSchedulerService : IScrapingSchedulerService
    {
        private readonly ScrapingOrchestrationDbContext _context;
        private readonly ILogger<ScrapingSchedulerService> _logger;

        // Default scraping frequencies
        private static readonly TimeSpan DefaultScrapingFrequency = TimeSpan.FromHours(4);
        private static readonly Dictionary<string, TimeSpan> FrequencyMapping = new()
        {
            ["PT1H"] = TimeSpan.FromHours(1),
            ["PT2H"] = TimeSpan.FromHours(2),
            ["PT4H"] = TimeSpan.FromHours(4),
            ["PT6H"] = TimeSpan.FromHours(6),
            ["PT12H"] = TimeSpan.FromHours(12),
            ["P1D"] = TimeSpan.FromDays(1),
            ["P2D"] = TimeSpan.FromDays(2),
            ["P7D"] = TimeSpan.FromDays(7)
        };

        public ScrapingSchedulerService(
            ScrapingOrchestrationDbContext context,
            ILogger<ScrapingSchedulerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProductSellerMapping>> GetMappingsDueForScrapingAsync(int maxMappings = 100)
        {
            var now = DateTimeOffset.UtcNow;

            var mappings = await _context.ProductSellerMappings
                .Include(m => m.SiteConfiguration)
                .Where(m => m.IsActiveForScraping && 
                           (m.NextScrapeAt == null || m.NextScrapeAt <= now))
                .OrderBy(m => m.NextScrapeAt ?? DateTimeOffset.MinValue)
                .Take(maxMappings)
                .ToListAsync();

            _logger.LogInformation("Found {Count} mappings due for scraping", mappings.Count);
            return mappings;
        }

        public async Task UpdateMappingScheduleAsync(Guid mappingId, DateTimeOffset? lastScrapedAt, DateTimeOffset? nextScrapeAt)
        {
            var mapping = await _context.ProductSellerMappings
                .FirstOrDefaultAsync(m => m.MappingId == mappingId);

            if (mapping == null)
            {
                _logger.LogWarning("Mapping {MappingId} not found for schedule update", mappingId);
                return;
            }

            if (lastScrapedAt.HasValue)
            {
                mapping.LastScrapedAt = lastScrapedAt.Value;
            }

            if (nextScrapeAt.HasValue)
            {
                mapping.NextScrapeAt = nextScrapeAt.Value;
            }
            else if (lastScrapedAt.HasValue)
            {
                // Calculate next scrape time based on frequency
                var frequency = await GetScrapingFrequencyAsync(mapping);
                mapping.NextScrapeAt = lastScrapedAt.Value.Add(frequency);
            }

            mapping.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated schedule for mapping {MappingId}. Next scrape: {NextScrape}", 
                mappingId, mapping.NextScrapeAt);
        }

        public async Task<TimeSpan> GetScrapingFrequencyAsync(ProductSellerMapping mapping)
        {
            // Check if mapping has a frequency override
            if (!string.IsNullOrEmpty(mapping.ScrapingFrequencyOverride))
            {
                if (FrequencyMapping.TryGetValue(mapping.ScrapingFrequencyOverride, out var overrideFrequency))
                {
                    return overrideFrequency;
                }
                
                // Try to parse as TimeSpan directly
                if (TimeSpan.TryParse(mapping.ScrapingFrequencyOverride, out var parsedFrequency))
                {
                    return parsedFrequency;
                }

                _logger.LogWarning("Invalid scraping frequency override '{Override}' for mapping {MappingId}", 
                    mapping.ScrapingFrequencyOverride, mapping.MappingId);
            }

            // Return default frequency
            return DefaultScrapingFrequency;
        }
    }
}
