using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace TechTicker.Shared.Attributes
{
    /// <summary>
    /// Attribute for requiring specific TechTicker roles
    /// </summary>
    public class TechTickerAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string[]? RequiredRoles { get; set; }
        public string[]? RequiredScopes { get; set; }
        public bool AllowServiceToService { get; set; } = false;

        public TechTickerAuthorizeAttribute()
        {
        }

        public TechTickerAuthorizeAttribute(params string[] roles)
        {
            RequiredRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if service-to-service communication is allowed and present
            if (AllowServiceToService && user.HasClaim("scope", "service-to-service"))
            {
                return; // Allow service-to-service calls
            }

            // Check required roles
            if (RequiredRoles?.Length > 0)
            {
                var userRoles = user.FindAll(ClaimTypes.Role)
                    .Concat(user.FindAll("role"))
                    .Select(c => c.Value)
                    .ToList();

                if (!RequiredRoles.Any(role => userRoles.Contains(role)))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // Check required scopes
            if (RequiredScopes?.Length > 0)
            {
                var userScopes = user.FindAll("scope")
                    .Select(c => c.Value)
                    .ToList();

                if (!RequiredScopes.Any(scope => userScopes.Contains(scope)))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Attribute for requiring admin access
    /// </summary>
    public class AdminOnlyAttribute : TechTickerAuthorizeAttribute
    {
        public AdminOnlyAttribute() : base("Admin")
        {
        }
    }

    /// <summary>
    /// Attribute for requiring user or admin access
    /// </summary>
    public class UserOrAdminAttribute : TechTickerAuthorizeAttribute
    {
        public UserOrAdminAttribute() : base("User", "Admin")
        {
        }
    }

    /// <summary>
    /// Attribute for allowing service-to-service communication
    /// </summary>
    public class ServiceToServiceAttribute : TechTickerAuthorizeAttribute
    {
        public ServiceToServiceAttribute()
        {
            AllowServiceToService = true;
            RequiredScopes = new[] { "service-to-service" };
        }
    }

    /// <summary>
    /// Attribute for read-only access
    /// </summary>
    public class ReadOnlyAccessAttribute : TechTickerAuthorizeAttribute
    {
        public ReadOnlyAccessAttribute()
        {
            RequiredScopes = new[] { "read" };
            AllowServiceToService = true;
        }
    }

    /// <summary>
    /// Attribute for write access
    /// </summary>
    public class WriteAccessAttribute : TechTickerAuthorizeAttribute
    {
        public WriteAccessAttribute()
        {
            RequiredScopes = new[] { "write" };
            RequiredRoles = new[] { "Admin" };
            AllowServiceToService = true;
        }
    }
}
