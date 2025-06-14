using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using System.Security.Claims;
using TechTicker.Shared.Models;
using TechTicker.UserService.DTOs;
using System.Collections.Immutable;

namespace TechTicker.UserService.Services
{
    /// <summary>
    /// Configuration options for JWT tokens
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        
        public string Issuer { get; set; } = "TechTicker";
        public string Audience { get; set; } = "TechTicker";
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 30;
    }

    /// <summary>
    /// Service for token generation and management using OpenIddict
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IOpenIddictTokenManager _tokenManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtOptions _jwtOptions;

        public TokenService(
            IOpenIddictTokenManager tokenManager,
            IOpenIddictApplicationManager applicationManager,
            ILogger<TokenService> logger,
            IOptions<JwtOptions> jwtOptions)
        {
            _tokenManager = tokenManager;
            _applicationManager = applicationManager;
            _logger = logger;
            _jwtOptions = jwtOptions.Value;
        }

        public Task<TokenResponse> GenerateAccessTokenAsync(User user)
        {
            try
            {
                // In a production OpenIddict setup, token generation is typically handled 
                // by the authorization endpoints (/connect/token) rather than directly here.
                // This method provides a simplified token response for compatibility with the UserService.
                
                _logger.LogInformation("Generating token response for user: {UserId}", user.UserId);

                // Create user response
                var userResponse = new UserResponse
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
                };

                // For direct API authentication, we return a success response.
                // Clients should use the OpenIddict endpoints (/connect/token) for OAuth2/OpenID Connect flows.
                return Task.FromResult(new TokenResponse
                {
                    AccessToken = "use_openiddict_endpoints", // Indicates to use /connect/token
                    TokenType = "Bearer",
                    ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60,
                    User = userResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token response for user: {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var storedToken = await _tokenManager.FindByReferenceIdAsync(token);
                return storedToken != null && 
                       await _tokenManager.GetExpirationDateAsync(storedToken) > DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                var storedToken = await _tokenManager.FindByReferenceIdAsync(token);
                if (storedToken != null)
                {
                    await _tokenManager.TryRevokeAsync(storedToken);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        /// <summary>
        /// Helper method to determine claim destinations for OpenIddict
        /// </summary>
        public static ImmutableArray<string> GetDestinations(Claim claim)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            return claim.Type switch
            {
                ClaimTypes.NameIdentifier => ImmutableArray.Create(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken),

                ClaimTypes.Email => ImmutableArray.Create(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken),

                ClaimTypes.Name => ImmutableArray.Create(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken),

                ClaimTypes.Role => ImmutableArray.Create(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken),

                ClaimTypes.GivenName or ClaimTypes.Surname => ImmutableArray.Create(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken),

                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                "AspNet.Identity.SecurityStamp" => ImmutableArray<string>.Empty,

                _ => ImmutableArray.Create(OpenIddictConstants.Destinations.AccessToken)
            };
        }
    }
}
