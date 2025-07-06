var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var techtickerDb = postgres.AddDatabase("techticker-db");

// Add RabbitMQ messaging
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume();

// Add Redis cache
var redis = builder.AddRedis("cache")
    .WithDataVolume();

// Add API Service
var apiService = builder.AddProject<Projects.TechTicker_ApiService>("apiservice")
    .WaitFor(rabbitmq)
    .WaitFor(redis)
    .WithReference(techtickerDb)
    .WithReference(rabbitmq)
    .WithReference(redis);

// Add Scraping Worker
var scrapingWorker = builder.AddProject<Projects.TechTicker_ScrapingWorker>("scrapingworker")
    .WaitFor(rabbitmq)
    .WithReference(techtickerDb)
    .WithReference(rabbitmq);

// Add Notification Worker
var notificationWorker = builder.AddProject<Projects.TechTicker_NotificationWorker>("notificationworker")
    .WaitFor(rabbitmq)
    .WithReference(techtickerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Discord__WebhookUrl", "")
    .WithEnvironment("Discord__BotName", "TechTicker")
    .WithEnvironment("Discord__EnableDiscordNotifications", "false"); // Disabled by default for development

builder.AddNpmApp("angular", "../TechTicker.Frontend")
    .WithReference(apiService)
    .WithHttpEndpoint(port: 4200, name: "http", env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
