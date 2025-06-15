var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume(isReadOnly: false)
    .WithManagementPlugin();;

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin();
var productDb = postgres.AddDatabase("product");
var userDb = postgres.AddDatabase("user");
var productSellerMappingDb = postgres.AddDatabase("product-seller-mapping");
var priceHistoryDb = postgres.AddDatabase("price-history");
var scrapingOrchestrationDb = postgres.AddDatabase("scraping-orchestration");

builder
    .AddNpmApp("AngularFrontEnd", "../../Services/TechTicker.UI")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

var productService = builder.AddProject<Projects.TechTicker_ProductService>("techticker-productservice")
    .WithReference(productDb)
    .WaitFor(postgres);

var userService = builder.AddProject<Projects.TechTicker_UserService>("techticker-userservice")
    .WithReference(userDb)
    .WaitFor(postgres);

var migrationService = builder.AddProject<Projects.TechTicker_MigrationService>("migrations")
    .WithReference(productDb)
    .WithReference(userDb)
    .WithReference(productSellerMappingDb)
    .WithReference(priceHistoryDb)
    .WithReference(scrapingOrchestrationDb)
    .WaitFor(postgres);

var productSellerMappingService = builder.AddProject<Projects.TechTicker_ProductSellerMappingService>("techticker-productsellermappingservice")
    .WithReference(productSellerMappingDb)
    .WithReference(productService)
    .WaitFor(postgres);

var scrapingOrchestrationService = builder.AddProject<Projects.TechTicker_ScrapingOrchestrationService>("techticker-scrapingorchestrationservice")
    .WithReference(scrapingOrchestrationDb)
    .WithReference(productSellerMappingDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

var scraperService = builder.AddProject<Projects.TechTicker_ScraperService>("techticker-scraperservice")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var priceNormalizationService = builder.AddProject<Projects.TechTicker_PriceNormalizationService>("techticker-pricenormalizationservice")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

var priceHistoryService = builder.AddProject<Projects.TechTicker_PriceHistoryService>("techticker-pricehistoryservice")
    .WithReference(priceHistoryDb)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

// API Gateway - waits for all API services to be ready before starting
var apiGateway = builder.AddProject<Projects.TechTicker_ReverseProxy>("techticker-apigateway")
    .WaitFor(productService)
    .WaitFor(userService)
    .WaitFor(productSellerMappingService)
    .WaitFor(priceHistoryService)
    .WaitFor(migrationService); // Wait for migrations to complete database setup

builder.Build().Run();
