using TechTicker.PriceHistoryService.Workers;
using TechTicker.PriceHistoryService.Services;
using TechTicker.PriceHistoryService.Data;
using TechTicker.Shared.Extensions;
using TechTicker.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace TechTicker.PriceHistoryService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Aspire service defaults (health checks, logging, telemetry)
        builder.AddServiceDefaults();

        // Add RabbitMQ client for message consumption
        builder.AddRabbitMQClient("messaging");

        // Add PostgreSQL database connection
        builder.AddNpgsqlDbContext<PriceHistoryDbContext>("pricehistorydb");

        // Add TechTicker shared services (Result pattern, common utilities)
        builder.Services.AddTechTickerShared();

        // Register application services
        builder.Services.AddScoped<IPriceHistoryService, Services.PriceHistoryService>();
        builder.Services.AddScoped<IMessageConsumerService, MessageConsumerService>();

        // Register the price history ingestion worker (background service)
        builder.Services.AddHostedService<PriceHistoryIngestionWorker>();

        // Add controllers for API endpoints
        builder.Services.AddControllers();

        // Add Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Map default health check endpoint
        app.MapDefaultEndpoints();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
