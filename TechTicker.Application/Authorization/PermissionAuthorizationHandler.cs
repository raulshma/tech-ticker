using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechTicker.DataAccess;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Authorization;

namespace TechTicker.Application.Authorization;

/// <summary>
/// Authorization handler for permission-based access control
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly TechTickerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionAuthorizationHandler(TechTickerDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        // First check if the permission is directly in the JWT claims (faster)
        var permissionClaims = context.User.FindAll("permission").Select(c => c.Value);
        if (permissionClaims.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // Fallback to database check (for cases where token doesn't have permission claims)
        var hasPermission = await HasPermissionAsync(userId, requirement.Permission);
        
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> HasPermissionAsync(Guid userId, string permissionName)
    {
        // Get user's roles
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Any())
        {
            return false;
        }

        // Check if any of the user's roles have the required permission
        var hasPermission = await _context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .AnyAsync(rp => userRoles.Contains(rp.Role.Name!) && 
                           rp.Permission.Name == permissionName);

        return hasPermission;
    }
}
