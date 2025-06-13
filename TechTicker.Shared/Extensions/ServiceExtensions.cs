using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TechTicker.Shared.Middleware;

namespace TechTicker.Shared.Extensions
{
    /// <summary>
    /// Extension methods for configuring TechTicker shared services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds TechTicker shared services to the DI container
        /// </summary>
        public static IServiceCollection AddTechTickerShared(this IServiceCollection services)
        {
            // Add any shared services here
            // For example: validation services, mapping services, etc.
            
            return services;
        }
    }

    /// <summary>
    /// Extension methods for configuring the application pipeline
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the global exception handling middleware to the pipeline
        /// </summary>
        public static IApplicationBuilder UseTechTickerExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }

        /// <summary>
        /// Adds correlation ID handling to the pipeline
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                const string correlationIdKey = "X-Correlation-ID";
                
                if (!context.Request.Headers.ContainsKey(correlationIdKey))
                {
                    context.Request.Headers.Add(correlationIdKey, Guid.NewGuid().ToString());
                }

                var correlationId = context.Request.Headers[correlationIdKey].ToString();
                context.Response.Headers.TryAdd(correlationIdKey, correlationId);

                await next();
            });
        }
    }
}
