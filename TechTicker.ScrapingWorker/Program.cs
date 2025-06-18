using TechTicker.Application.Configuration;
using TechTicker.Application.MessageHandlers;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.ScrapingWorker;
using TechTicker.ScrapingWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add database context
builder.AddNpgsqlDbContext<TechTickerDbContext>("techticker-db");

// Add RabbitMQ client
builder.AddRabbitMQClient("messaging");

// Configure messaging
builder.Services.Configure<MessagingConfiguration>(
    builder.Configuration.GetSection(MessagingConfiguration.SectionName));

// Configure ProductDiscovery options
builder.Services.AddOptions<ProductDiscoveryOptions>()
    .Bind(builder.Configuration.GetSection(ProductDiscoveryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register individual repositories for direct injection
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductSellerMappingRepository, ProductSellerMappingRepository>();
builder.Services.AddScoped<IScraperSiteConfigurationRepository, ScraperSiteConfigurationRepository>();
builder.Services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
builder.Services.AddScoped<IScraperRunLogRepository, ScraperRunLogRepository>();
builder.Services.AddScoped<IProductDiscoveryCandidateRepository, ProductDiscoveryCandidateRepository>();
builder.Services.AddScoped<IDiscoveryApprovalWorkflowRepository, DiscoveryApprovalWorkflowRepository>();
builder.Services.AddScoped<ISiteConfigurationRepository, SiteConfigurationRepository>();

// Register application services
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IScrapingOrchestrationService, ScrapingOrchestrationService>();
builder.Services.AddScoped<IScraperRunLogService, ScraperRunLogService>();

// Add Product Discovery services
builder.Services.AddScoped<IProductDiscoveryService, ProductDiscoveryService>();
builder.Services.AddScoped<IUrlAnalysisService, UrlAnalysisService>();
builder.Services.AddScoped<ICategoryPredictionService, CategoryPredictionService>();
builder.Services.AddScoped<IProductSimilarityService, ProductSimilarityService>();
builder.Services.AddScoped<IDiscoveryWorkflowService, DiscoveryWorkflowService>();
builder.Services.AddScoped<ISiteConfigurationService, SiteConfigurationService>();
builder.Services.AddScoped<IAISelectorGenerationService, AISelectorGenerationService>();

// Add HttpClient for AI service
builder.Services.AddHttpClient<AISelectorGenerationService>();

// Add message handlers
builder.Services.AddScoped<ProductDiscoveryEventHandler>();

// Add messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();

// Add scraping services
builder.Services.AddScoped<WebScrapingService>();
builder.Services.AddScoped<PriceDataProcessingService>();

// Add the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
