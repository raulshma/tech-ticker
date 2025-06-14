using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace TechTicker.Shared.Middleware
{
    /// <summary>
    /// Middleware for extracting and enriching user context from authentication tokens
    /// </summary>
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserContextMiddleware> _logger;

        public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract user information from the authenticated user
            if (context.User.Identity?.IsAuthenticated == true)
            {
                ExtractUserContext(context);
            }

            await _next(context);
        }

        private void ExtractUserContext(HttpContext context)
        {
            try
            {
                var user = context.User;
                
                // Extract user ID
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.FindFirst("id")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    context.Items["UserId"] = userId;
                }

                // Extract user email
                var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;

                if (!string.IsNullOrEmpty(email))
                {
                    context.Items["UserEmail"] = email;
                }

                // Extract user roles
                var roles = user.FindAll(ClaimTypes.Role)
                    .Concat(user.FindAll("role"))
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList();

                if (roles.Any())
                {
                    context.Items["UserRoles"] = roles;
                }

                // Extract scopes
                var scopes = user.FindAll("scope")
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList();

                if (scopes.Any())
                {
                    context.Items["UserScopes"] = scopes;
                }

                // Extract user name
                var userName = user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst("name")?.Value
                    ?? user.FindFirst("preferred_username")?.Value;

                if (!string.IsNullOrEmpty(userName))
                {
                    context.Items["UserName"] = userName;
                }

                _logger.LogDebug("User context extracted for user: {UserId}, Email: {Email}, Roles: {Roles}", 
                    userId, email, string.Join(", ", roles));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user context from claims");
            }
        }
    }

    /// <summary>
    /// Extension methods for registering the UserContextMiddleware
    /// </summary>
    public static class UserContextMiddlewareExtensions
    {
        /// <summary>
        /// Adds the user context middleware to the application pipeline
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseTechTickerUserContext(this IApplicationBuilder app)
        {
            return app.UseMiddleware<UserContextMiddleware>();
        }
    }
}
