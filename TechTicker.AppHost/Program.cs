var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var techtickerDb = postgres.AddDatabase("techticker-db");

// Add RabbitMQ messaging
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

// Add API Service
var apiService = builder.AddProject<Projects.TechTicker_ApiService>("apiservice")
    .WithReference(techtickerDb)
    .WithReference(rabbitmq);

// Add Scraping Worker
var scrapingWorker = builder.AddProject<Projects.TechTicker_ScrapingWorker>("scrapingworker")
    .WithReference(techtickerDb)
    .WithReference(rabbitmq);

// Add Notification Worker
var notificationWorker = builder.AddProject<Projects.TechTicker_NotificationWorker>("notificationworker")
    .WithReference(techtickerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Email__SmtpHost", "localhost")
    .WithEnvironment("Email__SmtpPort", "587")
    .WithEnvironment("Email__UseSsl", "true")
    .WithEnvironment("Email__EnableEmailSending", "false"); // Disabled by default for development

builder.Build().Run();
