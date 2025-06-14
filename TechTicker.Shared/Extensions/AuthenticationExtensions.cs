using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using TechTicker.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TechTicker.Shared.Extensions
{
    /// <summary>
    /// Extension methods for configuring TechTicker authentication and authorization
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds TechTicker OpenIddict-based authentication to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The configuration section name (defaults to "OpenIddictAuthentication")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerAuthentication(
            this IServiceCollection services, 
            IConfiguration configuration, 
            string sectionName = OpenIddictAuthenticationSettings.SectionName)
        {
            var authSettings = configuration.GetSection(sectionName).Get<OpenIddictAuthenticationSettings>() 
                ?? new OpenIddictAuthenticationSettings();
            
            services.Configure<OpenIddictAuthenticationSettings>(configuration.GetSection(sectionName));
            
            // Validate required settings
            if (string.IsNullOrEmpty(authSettings.AuthorizationServerUrl))
            {
                throw new InvalidOperationException($"AuthorizationServerUrl is required in {sectionName} configuration section");
            }

            // Add OpenIddict validation
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Configure the OpenIddict validation handler to use the authorization server
                    options.SetIssuer(authSettings.AuthorizationServerUrl);
                    
                    if (!string.IsNullOrEmpty(authSettings.Audience))
                    {
                        options.AddAudiences(authSettings.Audience);
                    }
                    
                    // Enable token introspection if configured
                    if (authSettings.EnableIntrospection && 
                        !string.IsNullOrEmpty(authSettings.ClientId) && 
                        !string.IsNullOrEmpty(authSettings.ClientSecret))
                    {
                        options.UseIntrospection()
                               .SetClientId(authSettings.ClientId)
                               .SetClientSecret(authSettings.ClientSecret);
                    }
                    else
                    {
                        // Use local validation with JWKS
                        options.UseSystemNetHttp();
                    }
                    
                    // Register the ASP.NET Core host
                    options.UseAspNetCore();
                });

            // Configure authentication schemes
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            return services;
        }

        /// <summary>
        /// Adds JWT Bearer authentication as an alternative to OpenIddict (for services that prefer JWT Bearer)
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The configuration section name (defaults to "JwtBearer")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "JwtBearer")
        {
            var jwtSettings = configuration.GetSection(sectionName).Get<JwtBearerSettings>()
                ?? new JwtBearerSettings();

            services.Configure<JwtBearerSettings>(configuration.GetSection(sectionName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = jwtSettings.Authority;
                options.Audience = jwtSettings.Audience;
                options.RequireHttpsMetadata = jwtSettings.RequireHttpsMetadata;
                
                if (!string.IsNullOrEmpty(jwtSettings.MetadataAddress))
                {
                    options.MetadataAddress = jwtSettings.MetadataAddress;
                }

                // For development: allow self-signed certificates
                if (jwtSettings.ValidateIssuerSigningKey && !string.IsNullOrEmpty(jwtSettings.SigningKey))
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Authority,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                }
                else
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero
                    };
                }

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogError(context.Exception, "Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogDebug("Token validated for user: {User}", 
                            context.Principal?.Identity?.Name ?? "Unknown");
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Adds TechTicker authorization policies to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="sectionName">The configuration section name (defaults to "AuthorizationPolicies")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerAuthorization(
            this IServiceCollection services, 
            IConfiguration configuration,
            string sectionName = AuthorizationPolicySettings.SectionName)
        {
            var authzSettings = configuration.GetSection(sectionName).Get<AuthorizationPolicySettings>() 
                ?? new AuthorizationPolicySettings();
                
            services.Configure<AuthorizationPolicySettings>(configuration.GetSection(sectionName));

            services.AddAuthorization(options =>
            {
                // Configure default policy
                if (authzSettings.RequireAuthenticatedUserByDefault)
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                }

                // Add common policies
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("UserOrAdmin", policy =>
                    policy.RequireRole("User", "Admin"));

                options.AddPolicy("ServiceToService", policy =>
                    policy.RequireClaim("scope", "service-to-service"));

                options.AddPolicy("ReadOnly", policy =>
                    policy.RequireAuthenticatedUser()
                           .RequireAssertion(context =>
                               context.User.HasClaim("scope", "read") ||
                               context.User.IsInRole("User") ||
                               context.User.IsInRole("Admin")));

                options.AddPolicy("WriteAccess", policy =>
                    policy.RequireAuthenticatedUser()
                           .RequireAssertion(context =>
                               context.User.HasClaim("scope", "write") ||
                               context.User.IsInRole("Admin")));

                // Add custom policies from configuration
                foreach (var (policyName, policyDef) in authzSettings.Policies)
                {
                    options.AddPolicy(policyName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        
                        if (policyDef.RequiredRoles.Any())
                        {
                            policy.RequireRole(policyDef.RequiredRoles.ToArray());
                        }
                        
                        foreach (var (claimType, claimValue) in policyDef.RequiredClaims)
                        {
                            policy.RequireClaim(claimType, claimValue);
                        }
                        
                        if (policyDef.RequiredScopes.Any())
                        {
                            policy.RequireClaim("scope", policyDef.RequiredScopes.ToArray());
                        }
                    });
                }
            });

            return services;
        }

        /// <summary>
        /// Adds both TechTicker authentication and authorization
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerAuth(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            return services
                .AddTechTickerAuthentication(configuration)
                .AddTechTickerAuthorization(configuration);
        }

        /// <summary>
        /// Adds TechTicker authentication for services that consume APIs (non-authentication servers)
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="isDevelopment">Whether this is a development environment</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTechTickerServiceAuth(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment = false)
        {
            // Configure HTTP clients for authentication
            services.ConfigureTechTickerHttpClients(isDevelopment);

            // Add authentication and authorization
            services.AddTechTickerAuth(configuration);

            return services;
        }
        
        /// <summary>
        /// Configures the HTTP client for OpenIddict validation
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="isDevelopment">Whether this is a development environment</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection ConfigureTechTickerHttpClients(
            this IServiceCollection services,
            bool isDevelopment = false)
        {
            services.AddHttpClient("TechTicker.OpenIddict", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "TechTicker/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                
                if (isDevelopment)
                {
                    // For development - allow self-signed certificates
                    handler.ServerCertificateCustomValidationCallback = 
                        (message, cert, chain, errors) => true;
                }
                
                return handler;
            });
            
            return services;
        }

        /// <summary>
        /// Creates default authentication configuration for services
        /// </summary>
        /// <param name="userServiceUrl">The URL of the User Service (authentication server)</param>
        /// <param name="audience">The audience for token validation</param>
        /// <param name="requireHttps">Whether to require HTTPS</param>
        /// <returns>A configuration dictionary</returns>
        public static Dictionary<string, object> CreateDefaultAuthConfig(
            string userServiceUrl, 
            string audience = "techticker-api", 
            bool requireHttps = true)
        {
            return new Dictionary<string, object>
            {
                [OpenIddictAuthenticationSettings.SectionName] = new
                {
                    AuthorizationServerUrl = userServiceUrl,
                    Audience = audience,
                    RequireHttpsMetadata = requireHttps,
                    EnableIntrospection = false
                },
                [AuthorizationPolicySettings.SectionName] = new
                {
                    RequireAuthenticatedUserByDefault = true,
                    DefaultScheme = "OpenIddict"
                }
            };
        }
    }
}
