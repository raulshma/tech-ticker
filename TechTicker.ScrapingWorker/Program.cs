using TechTicker.Application.Configuration;
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

// Add repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IScrapingOrchestrationService, ScrapingOrchestrationService>();
builder.Services.AddScoped<IScraperRunLogService, ScraperRunLogService>();

// Add messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();

// Add HTTP client for web scraping
builder.Services.AddHttpClient<WebScrapingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add scraping services
builder.Services.AddScoped<WebScrapingService>();
builder.Services.AddScoped<PriceDataProcessingService>();

// Add the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
