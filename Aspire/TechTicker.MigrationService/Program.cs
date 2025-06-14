using TechTicker.MigrationService;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductService.Data;
using TechTicker.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<ProductDbContext>("product");
builder.AddNpgsqlDbContext<ProductSellerMappingDbContext>("product-seller-mapping");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
