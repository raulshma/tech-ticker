using Microsoft.AspNetCore.Authorization;

namespace TechTicker.Shared.Authorization;

/// <summary>
/// Authorization attribute that requires a specific permission
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission.{permission}";
    }
}
