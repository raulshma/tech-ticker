using OpenIddict.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechTicker.UserService.Data;
using Microsoft.EntityFrameworkCore;

namespace TechTicker.UserService.Workers
{
    /// <summary>
    /// Worker service to seed OpenIddict data on startup
    /// </summary>
    public class OpenIddictWorker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OpenIddictWorker> _logger;

        public OpenIddictWorker(
            IServiceProvider serviceProvider,
            ILogger<OpenIddictWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            // Ensure database is created and up to date
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            // Create default application
            if (await applicationManager.FindByClientIdAsync("TechTicker.Client", cancellationToken) == null)
            {
                await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "TechTicker.Client",
                    ClientSecret = "TechTicker.Secret", // In production, use a secure secret
                    ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                    DisplayName = "TechTicker Client",
                    ClientType = OpenIddictConstants.ClientTypes.Confidential,
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.Endpoints.Logout,
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.ResponseTypes.Code,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Roles
                    },
                    Requirements =
                    {
                        OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                    }
                }, cancellationToken);

                _logger.LogInformation("Created default OpenIddict application");
            }

            // Create scopes
            if (await scopeManager.FindByNameAsync("email", cancellationToken) == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = OpenIddictConstants.Scopes.Email,
                    DisplayName = "Email access",
                    Resources =
                    {
                        "TechTicker.UserService"
                    }
                }, cancellationToken);
            }

            if (await scopeManager.FindByNameAsync("profile", cancellationToken) == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = OpenIddictConstants.Scopes.Profile,
                    DisplayName = "Profile access",
                    Resources =
                    {
                        "TechTicker.UserService"
                    }
                }, cancellationToken);
            }

            if (await scopeManager.FindByNameAsync("roles", cancellationToken) == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "roles",
                    DisplayName = "Roles access",
                    Resources =
                    {
                        "TechTicker.UserService"
                    }
                }, cancellationToken);
            }

            _logger.LogInformation("OpenIddict seeding completed");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
