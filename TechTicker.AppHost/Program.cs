var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var techtickerDb = postgres.AddDatabase("techticker-db");

var username = builder.AddParameter("username", "guest", secret: true);
var password = builder.AddParameter("password", "guest",secret: true);
// Add RabbitMQ messaging
var rabbitmq = builder.AddRabbitMQ("messaging", username, password)
    .WithDataVolume()
    .WithManagementPlugin();

// Add API Service
var apiService = builder.AddProject<Projects.TechTicker_ApiService>("apiservice")
    .WithReference(techtickerDb)
    .WithReference(rabbitmq);

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
    .WithEnvironment("Email__SmtpHost", "localhost")
    .WithEnvironment("Email__SmtpPort", "587")
    .WithEnvironment("Email__UseSsl", "true")
    .WithEnvironment("Email__EnableEmailSending", "false"); // Disabled by default for development

builder.AddNpmApp("angular", "../TechTicker.Frontend")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(port: 4200, name: "http", env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
