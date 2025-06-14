using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
        
        /// <summary>
        /// Adds TechTicker shared services with authentication to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="enableAuthentication">Whether to enable authentication (default: true)</param>
        /// <param name="isDevelopment">Whether this is a development environment</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerShared(
            this IServiceCollection services, 
            IConfiguration configuration,
            bool enableAuthentication = true,
            bool isDevelopment = false)
        {
            // Add base shared services
            services.AddTechTickerShared();
            
            if (enableAuthentication)
            {
                // Add authentication and authorization
                services.AddTechTickerServiceAuth(configuration, isDevelopment);
            }
            
            return services;
        }

        /// <summary>
        /// Adds TechTicker services with authentication server capability
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="isDevelopment">Whether this is a development environment</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerAuthServer(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment = false)
        {
            // Add base shared services
            services.AddTechTickerShared();
            
            // Note: Authentication server (UserService) has its own OpenIddict server configuration
            // This method is for services that need to validate tokens from the auth server
            services.AddTechTickerAuthorization(configuration);
            
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
        /// Adds the complete TechTicker middleware pipeline
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="enableAuthentication">Whether to add authentication middleware</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseTechTickerPipeline(
            this IApplicationBuilder app,
            bool enableAuthentication = true)
        {
            // Add correlation ID handling first
            app.UseCorrelationId();
            
            // Add exception handling
            app.UseTechTickerExceptionHandling();
            
            if (enableAuthentication)
            {
                // Add authentication and authorization
                app.UseAuthentication();
                app.UseAuthorization();
                
                // Add user context extraction
                app.UseTechTickerUserContext();
            }
            
            return app;
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
                    context.Request.Headers[correlationIdKey] = Guid.NewGuid().ToString();
                }

                var correlationId = context.Request.Headers[correlationIdKey].ToString();
                context.Response.Headers.TryAdd(correlationIdKey, correlationId);

                await next();
            });
        }
    }
}
