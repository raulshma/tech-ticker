
using Scalar.AspNetCore;
using TechTicker.UserService.Data;
using TechTicker.UserService.Services;
using TechTicker.Shared.Extensions;
using TechTicker.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace TechTicker.UserService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);        // Add database
        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<UserDbContext>("user", configureDbContextOptions: options =>
        {
            options.UseOpenIddict();
        });
        builder.Services.AddHttpLogging(o => { });

        // Add TechTicker shared services
        builder.Services.AddTechTickerShared();

        // Add services to the container
        builder.Services.AddControllers();

        // Configure OpenIddict
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<UserDbContext>();
            })
            .AddServer(options =>
            {
                // Enable the authorization, logout, token and userinfo endpoints.
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetLogoutEndpointUris("/connect/logout")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserinfoEndpointUris("/connect/userinfo");

                // Mark the "email", "profile" and "roles" scopes as supported scopes.
                options.RegisterScopes("email", "profile", "roles");

                // Note: this sample only uses the authorization code flow but you can enable
                // the other flows if you need to support implicit, password or client credentials.
                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .AllowClientCredentialsFlow()
                       .AllowPasswordFlow(); // Enable password grant for direct login

                // Register the signing and encryption credentials.
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableLogoutEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserinfoEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();
            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });        // Register application services
        builder.Services.AddScoped<IUserService, Services.UserService>();
        builder.Services.AddScoped<ITokenService, TechTicker.UserService.Services.TokenService>();

        // Register the OpenIddict seeding worker
        builder.Services.AddHostedService<TechTicker.UserService.Workers.OpenIddictWorker>();        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(); // scalar/v1
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpLogging();

        // Add TechTicker shared middleware (should be early in pipeline)
        app.UseTechTickerExceptionHandling();
        app.UseCorrelationId();

        app.UseHttpsRedirection();

        // Add OpenIddict middleware
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
