using TechTicker.Application.Configuration;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.ScrapingWorker;
using TechTicker.ScrapingWorker.Configuration;
using TechTicker.ScrapingWorker.Services;
using TechTicker.ScrapingWorker.Services.Interfaces;
using TechTicker.Shared.Utilities.Html;

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

// Configure proxy pool
builder.Services.Configure<ProxyPoolConfiguration>(
    builder.Configuration.GetSection(ProxyPoolConfiguration.SectionName));

// Configure retry policies
builder.Services.Configure<RetryConfiguration>(
    builder.Configuration.GetSection("RetryConfiguration"));

// Add repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IScrapingOrchestrationService, ScrapingOrchestrationService>();
builder.Services.AddScoped<IScraperRunLogService, ScraperRunLogService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();

// Add messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();

// Add proxy services
builder.Services.AddScoped<IProxyPoolService, ProxyPoolService>();
builder.Services.AddScoped<IRetryPolicyService, RetryPolicyService>();
builder.Services.AddScoped<ProxyAwareHttpClientService>();
builder.Services.AddMemoryCache();

// Add HTTP client factory for proxy-aware requests
builder.Services.AddHttpClient();

// Add HTTP client for fallback scenarios
builder.Services.AddHttpClient("fallback", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add image storage and scraping services
builder.Services.AddScoped<TechTicker.Shared.Services.IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IImageScrapingService, ImageScrapingService>();

// Add scraping services
builder.Services.AddScoped<WebScrapingService>();
builder.Services.AddScoped<PriceDataProcessingService>();

// Register specification parsing services
builder.Services.AddSingleton<ITableParser, UniversalTableParser>();

// Register specification normalizer
builder.Services.AddSingleton<ISpecificationNormalizer, SpecificationNormalizer>();

// Add the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
