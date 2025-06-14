using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using TechTicker.UserService.Services;
using TechTicker.UserService.DTOs;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Utilities;

namespace TechTicker.UserService.Controllers
{
    /// <summary>
    /// Controller for OpenIddict authentication endpoints
    /// </summary>
    [ApiController]
    [Route("connect")]
    public class AuthController : BaseApiController
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// OAuth2 token endpoint
        /// </summary>
        [HttpPost("token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ?? 
                         throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {
                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // Subject (sub) is a required field, we use the client id as the subject identifier here.
                identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId ?? string.Empty);

                // Add some custom claims (optional).
                identity.AddClaim("some-claim", "some-value", OpenIddictConstants.Destinations.AccessToken);

                var principal = new ClaimsPrincipal(identity);

                // Ask OpenIddict to generate a new token and return an OAuth2 token response.
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsPasswordGrantType())
            {
                // Validate the username/password parameters and ensure the account is not locked out.
                var loginRequest = new LoginRequest
                {
                    Email = request.Username ?? string.Empty,
                    Password = request.Password ?? string.Empty
                };

                var result = await _userService.LoginAsync(loginRequest);
                
                if (result.IsFailure)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    OpenIddictConstants.Claims.Name,
                    OpenIddictConstants.Claims.Role);

                // Add the claims that will be persisted in the tokens.
                identity.AddClaim(OpenIddictConstants.Claims.Subject, result.Data!.User.UserId.ToString())
                        .AddClaim(OpenIddictConstants.Claims.Email, result.Data.User.Email)
                        .AddClaim(OpenIddictConstants.Claims.Name, result.Data.User.Email)
                        .AddClaim(OpenIddictConstants.Claims.PreferredUsername, result.Data.User.Email);

                if (!string.IsNullOrEmpty(result.Data.User.FirstName))
                {
                    identity.AddClaim(OpenIddictConstants.Claims.GivenName, result.Data.User.FirstName);
                }

                if (!string.IsNullOrEmpty(result.Data.User.LastName))
                {
                    identity.AddClaim(OpenIddictConstants.Claims.FamilyName, result.Data.User.LastName);
                }

                // Set the list of scopes granted to the client application.
                identity.AddClaim(OpenIddictConstants.Claims.Scope, OpenIddictConstants.Scopes.Email);
                identity.AddClaim(OpenIddictConstants.Claims.Scope, OpenIddictConstants.Scopes.Profile);
                identity.AddClaim(OpenIddictConstants.Claims.Scope, "roles");

                foreach (var claim in identity.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, identity));
                }

                var principal = new ClaimsPrincipal(identity);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the refresh token.
                var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

                // Retrieve the user profile corresponding to the refresh token.
                var userIdString = principal?.GetClaim(OpenIddictConstants.Claims.Subject);
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                var userResult = await _userService.GetUserByIdAsync(userId);
                if (userResult.IsFailure || !userResult.Data!.IsActive)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Ensure the user is still allowed to sign in.
                foreach (var claim in principal!.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal.Identity as ClaimsIdentity));
                }

                // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }

        /// <summary>
        /// OAuth2 userinfo endpoint
        /// </summary>
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("userinfo"), HttpPost("userinfo")]
        public async Task<IActionResult> Userinfo()
        {
            var userIdString = User.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest(new
                {
                    error = OpenIddictConstants.Errors.InvalidToken,
                    error_description = "The specified access token is not valid."
                });
            }

            var result = await _userService.GetUserByIdAsync(userId);
            if (result.IsFailure)
            {
                return BadRequest(new
                {
                    error = OpenIddictConstants.Errors.InvalidToken,
                    error_description = "The specified access token is not valid."
                });
            }

            var user = result.Data!;
            var claims = new Dictionary<string, object>
            {
                [OpenIddictConstants.Claims.Subject] = user.UserId.ToString(),
                [OpenIddictConstants.Claims.Email] = user.Email,
                [OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed
            };

            if (!string.IsNullOrEmpty(user.FirstName))
            {
                claims[OpenIddictConstants.Claims.GivenName] = user.FirstName;
            }

            if (!string.IsNullOrEmpty(user.LastName))
            {
                claims[OpenIddictConstants.Claims.FamilyName] = user.LastName;
            }

            if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
            {
                claims[OpenIddictConstants.Claims.Name] = $"{user.FirstName} {user.LastName}".Trim();
            }

            // Note: the complete list of standard claims supported by the OpenID Connect specification
            // can be found here: http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims

            return Ok(claims);
        }

        /// <summary>
        /// Simple registration endpoint for testing
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var result = await _userService.RegisterUserAsync(request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok(result.Data);
        }

        private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity? identity)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            switch (claim.Type)
            {
                case OpenIddictConstants.Claims.Name:
                    yield return OpenIddictConstants.Destinations.AccessToken;

                    if (identity?.HasScope(OpenIddictConstants.Scopes.Profile) == true)
                        yield return OpenIddictConstants.Destinations.IdentityToken;

                    yield break;

                case OpenIddictConstants.Claims.Email:
                    yield return OpenIddictConstants.Destinations.AccessToken;

                    if (identity?.HasScope(OpenIddictConstants.Scopes.Email) == true)
                        yield return OpenIddictConstants.Destinations.IdentityToken;

                    yield break;

                case OpenIddictConstants.Claims.Role:
                    yield return OpenIddictConstants.Destinations.AccessToken;

                    if (identity?.HasScope("roles") == true)
                        yield return OpenIddictConstants.Destinations.IdentityToken;

                    yield break;

                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                case "AspNet.Identity.SecurityStamp": yield break;

                default:
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield break;
            }
        }
    }
}
