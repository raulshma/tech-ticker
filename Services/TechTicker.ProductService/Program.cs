
using Scalar.AspNetCore;
using TechTicker.ProductService.Data;
using TechTicker.ProductService.Services;
using TechTicker.ProductService.Grpc;
using TechTicker.Shared.Extensions;
using TechTicker.ServiceDefaults;

namespace TechTicker.ProductService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddNpgsqlDbContext<ProductDbContext>("product");
        builder.AddServiceDefaults();
        builder.Services.AddHttpLogging(o => { });
        
        // Add services to the container.
        builder.Services.AddControllers();
        
        // Add gRPC services
        builder.Services.AddGrpc();
        
        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();
        
        // Register application services
        builder.Services.AddScoped<IProductService, Services.ProductService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

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
        app.MapGrpcService<ProductGrpcServiceImpl>();

        app.Run();
    }
}
