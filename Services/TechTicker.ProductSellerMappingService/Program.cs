using Scalar.AspNetCore;
using TechTicker.ProductSellerMappingService.Data;
using TechTicker.ProductSellerMappingService.Services;
using TechTicker.ProductSellerMappingService.Grpc;
using TechTicker.Shared.Extensions;
using TechTicker.Grpc.Clients.Products;
using TechTicker.Grpc.Contracts.Products;
using TechTicker.Shared.Utilities;
using TechTicker.ServiceDefaults;

namespace TechTicker.ProductSellerMappingService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add database
        builder.AddNpgsqlDbContext<ProductSellerMappingDbContext>("product-seller-mapping");

        builder.AddServiceDefaults();
        builder.Services.AddHttpLogging(o => { });

        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Add services to the container
        builder.Services.AddControllers();
        // Add gRPC services
        builder.Services.AddGrpc();

        var productServiceEndpoint = ServiceDiscoveryUtilities.GetServiceEndpoint("techticker-productservice", "https") ?? "https://localhost:7031";
        // Register gRPC clients
        builder.Services.AddGrpcClient<ProductGrpcService.ProductGrpcServiceClient>(options =>
        {
            options.Address = new Uri(productServiceEndpoint); // ProductService address - should be configured
        });        // Register application services
        builder.Services.AddScoped<IProductSellerMappingService, Services.ProductSellerMappingService>();
        builder.Services.AddScoped<IScraperSiteConfigurationService, ScraperSiteConfigurationService>();
        builder.Services.AddScoped<IProductGrpcClient, ProductGrpcClient>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(); // scalar/v1
            app.MapOpenApi();
        }

        app.UseHttpLogging();

        // Add TechTicker shared middleware (should be early in pipeline)
        app.UseTechTickerExceptionHandling();
        app.UseCorrelationId();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Map HTTP controllers
        app.MapControllers();

        // Map gRPC services
        app.MapGrpcService<MappingGrpcServiceImpl>();

        app.Run();
    }
}
