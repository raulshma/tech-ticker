
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
        
        // Add TechTicker shared services with authentication
        builder.Services.AddTechTickerShared(
            builder.Configuration, 
            enableAuthentication: true, 
            isDevelopment: builder.Environment.IsDevelopment());
        
        // Register application services
        builder.Services.AddScoped<IProductService, Services.ProductService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(); // scalar/v1
            app.MapOpenApi();
        }
        
        app.UseHttpLogging();
        
        // Add TechTicker shared middleware pipeline (includes auth)
        app.UseTechTickerPipeline(enableAuthentication: true);
        
        app.UseHttpsRedirection();
        
        // Map HTTP controllers
        app.MapControllers();
        
        // Map gRPC services
        app.MapGrpcService<ProductGrpcServiceImpl>();

        app.Run();
    }
}
