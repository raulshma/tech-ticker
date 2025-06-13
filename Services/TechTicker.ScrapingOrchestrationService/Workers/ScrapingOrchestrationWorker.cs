using TechTicker.ScrapingOrchestrationService.Services;
using TechTicker.ScrapingOrchestrationService.Messages;
using TechTicker.Shared.Models;

namespace TechTicker.ScrapingOrchestrationService.Workers
{
    public class ScrapingOrchestrationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScrapingOrchestrationWorker> _logger;
        private readonly IConfiguration _configuration;
        
        // Configuration settings
        private readonly TimeSpan _orchestrationInterval;
        private readonly int _maxMappingsPerCycle;
        private readonly int _maxConcurrentDomains;

        public ScrapingOrchestrationWorker(
            IServiceProvider serviceProvider,
            ILogger<ScrapingOrchestrationWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // Load configuration settings
            _orchestrationInterval = TimeSpan.FromMinutes(
                _configuration.GetValue<int>("ScrapingOrchestration:IntervalMinutes", 5));
            _maxMappingsPerCycle = _configuration.GetValue<int>("ScrapingOrchestration:MaxMappingsPerCycle", 50);
            _maxConcurrentDomains = _configuration.GetValue<int>("ScrapingOrchestration:MaxConcurrentDomains", 10);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping Orchestration Worker started. Interval: {Interval}, Max mappings per cycle: {MaxMappings}", 
                _orchestrationInterval, _maxMappingsPerCycle);

            // Initialize message publisher
            using var scope = _serviceProvider.CreateScope();
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisherService>();
            await messagePublisher.InitializeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScrapingCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during scraping orchestration cycle");
                }

                try
                {
                    await Task.Delay(_orchestrationInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Scraping Orchestration Worker stopping...");
                    break;
                }
            }
        }

        private async Task ProcessScrapingCycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var schedulerService = scope.ServiceProvider.GetRequiredService<IScrapingSchedulerService>();
            var domainProfileService = scope.ServiceProvider.GetRequiredService<IDomainScrapingProfileService>();
            var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisherService>();

            _logger.LogDebug("Starting scraping orchestration cycle");

            // Get mappings due for scraping
            var mappingsDue = await schedulerService.GetMappingsDueForScrapingAsync(_maxMappingsPerCycle);
            
            if (mappingsDue.Count == 0)
            {
                _logger.LogDebug("No mappings due for scraping");
                return;
            }

            // Group mappings by domain to manage request rates
            var mappingsByDomain = mappingsDue
                .GroupBy(m => ExtractDomain(m.ExactProductUrl))
                .Take(_maxConcurrentDomains)
                .ToList();

            _logger.LogInformation("Processing {MappingCount} mappings across {DomainCount} domains", 
                mappingsDue.Count, mappingsByDomain.Count);

            // Process each domain group
            var processingTasks = mappingsByDomain.Select(domainGroup => 
                ProcessDomainMappingsAsync(domainGroup.Key, domainGroup.ToList(), 
                    schedulerService, domainProfileService, messagePublisher, cancellationToken));

            await Task.WhenAll(processingTasks);

            _logger.LogDebug("Completed scraping orchestration cycle");
        }

        private async Task ProcessDomainMappingsAsync(
            string domain,
            List<ProductSellerMapping> mappings,
            IScrapingSchedulerService schedulerService,
            IDomainScrapingProfileService domainProfileService,
            IMessagePublisherService messagePublisher,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing {Count} mappings for domain {Domain}", mappings.Count, domain);

            foreach (var mapping in mappings)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Check if we can make a request to this domain
                    if (!await domainProfileService.CanMakeRequestAsync(domain))
                    {
                        _logger.LogDebug("Domain {Domain} is rate-limited, skipping mapping {MappingId}", 
                            domain, mapping.MappingId);
                        continue;
                    }

                    // Validate that we have site configuration
                    if (mapping.SiteConfiguration == null)
                    {
                        _logger.LogWarning("Mapping {MappingId} has no site configuration, skipping", mapping.MappingId);
                        continue;
                    }

                    // Get scraping configuration for this domain
                    var (userAgent, headers) = await domainProfileService.GetScrapingConfigurationAsync(domain);

                    // Create and publish scrape command
                    var command = new ScrapeProductPageCommand
                    {
                        MappingId = mapping.MappingId,
                        CanonicalProductId = mapping.CanonicalProductId,
                        SellerName = mapping.SellerName,
                        ExactProductUrl = mapping.ExactProductUrl,
                        Selectors = new ScrapingSelectors
                        {
                            ProductNameSelector = mapping.SiteConfiguration.ProductNameSelector,
                            PriceSelector = mapping.SiteConfiguration.PriceSelector,
                            StockSelector = mapping.SiteConfiguration.StockSelector,
                            SellerNameOnPageSelector = mapping.SiteConfiguration.SellerNameOnPageSelector
                        },
                        ScrapingProfile = new ScrapingProfile
                        {
                            UserAgent = userAgent,
                            Headers = headers
                        },
                        ScheduledAt = DateTimeOffset.UtcNow
                    };

                    await messagePublisher.PublishScrapeCommandAsync(command);

                    // Update domain request time and mapping schedule
                    await domainProfileService.UpdateLastRequestTimeAsync(domain);
                    
                    var now = DateTimeOffset.UtcNow;
                    var frequency = await schedulerService.GetScrapingFrequencyAsync(mapping);
                    var nextScrapeTime = now.Add(frequency);
                    
                    await schedulerService.UpdateMappingScheduleAsync(mapping.MappingId, now, nextScrapeTime);

                    _logger.LogDebug("Scheduled scraping for mapping {MappingId} (Product: {ProductId}, Seller: {Seller})", 
                        mapping.MappingId, mapping.CanonicalProductId, mapping.SellerName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing mapping {MappingId} for domain {Domain}", 
                        mapping.MappingId, domain);
                }
            }
        }

        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract domain from URL: {Url}", url);
                return "unknown";
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Scraping Orchestration Worker is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
