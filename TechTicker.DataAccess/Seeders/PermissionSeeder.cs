using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Constants;

namespace TechTicker.DataAccess.Seeders;

/// <summary>
/// Seeder for permissions and role-permission mappings
/// </summary>
public static class PermissionSeeder
{
    /// <summary>
    /// Seeds permissions and role-permission mappings
    /// </summary>
    public static async Task SeedAsync(
        TechTickerDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger)
    {
        try
        {
            // Seed permissions first
            await SeedPermissionsAsync(context, logger);

            // Seed role-permission mappings
            await SeedRolePermissionsAsync(context, roleManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding permissions");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(TechTickerDbContext context, ILogger logger)
    {
        var permissionsByCategory = Permissions.GetPermissionsByCategory();
        var existingPermissions = await context.Permissions.Select(p => p.Name).ToListAsync();

        var permissionsToAdd = new List<Permission>();

        foreach (var (category, permissions) in permissionsByCategory)
        {
            foreach (var permissionName in permissions)
            {
                if (!existingPermissions.Contains(permissionName))
                {
                    var permission = new Permission
                    {
                        PermissionId = Guid.NewGuid(),
                        Name = permissionName,
                        Description = GetPermissionDescription(permissionName),
                        Category = category,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    permissionsToAdd.Add(permission);
                }
            }
        }

        if (permissionsToAdd.Any())
        {
            context.Permissions.AddRange(permissionsToAdd);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} permissions", permissionsToAdd.Count);
        }
        else
        {
            logger.LogInformation("All permissions already exist, skipping permission seeding");
        }
    }

    private static async Task SeedRolePermissionsAsync(
        TechTickerDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger)
    {
        var rolePermissionMappings = GetDefaultRolePermissionMappings();
        var existingRolePermissions = await context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .ToListAsync();

        var rolePermissionsToAdd = new List<RolePermission>();

        foreach (var (roleName, permissionNames) in rolePermissionMappings)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                logger.LogWarning("Role {RoleName} not found, skipping permission assignments", roleName);
                continue;
            }

            var permissions = await context.Permissions
                .Where(p => permissionNames.Contains(p.Name))
                .ToListAsync();

            foreach (var permission in permissions)
            {
                var existingAssignment = existingRolePermissions
                    .FirstOrDefault(rp => rp.RoleId == role.Id && rp.PermissionId == permission.PermissionId);

                if (existingAssignment == null)
                {
                    var rolePermission = new RolePermission
                    {
                        RolePermissionId = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permission.PermissionId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    rolePermissionsToAdd.Add(rolePermission);
                }
            }
        }

        if (rolePermissionsToAdd.Any())
        {
            context.RolePermissions.AddRange(rolePermissionsToAdd);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} role-permission mappings", rolePermissionsToAdd.Count);
        }
        else
        {
            logger.LogInformation("All role-permission mappings already exist, skipping seeding");
        }
    }

    private static Dictionary<string, string[]> GetDefaultRolePermissionMappings()
    {
        return new Dictionary<string, string[]>
        {
            ["Admin"] = Permissions.GetAllPermissions(), // Admin gets all permissions
            ["User"] = new[]
            {
                // Product permissions
                Permissions.ProductsRead,
                
                // Price monitoring permissions
                Permissions.PriceHistoryRead,
                Permissions.AlertRulesCreate,
                Permissions.AlertRulesRead,
                Permissions.AlertRulesUpdate,
                Permissions.AlertRulesDelete
            },
            ["Moderator"] = new[]
            {
                // Product permissions
                Permissions.ProductsCreate,
                Permissions.ProductsRead,
                Permissions.ProductsUpdate,
                Permissions.ProductsManageCategories,
                
                // Price monitoring permissions
                Permissions.PriceHistoryRead,
                Permissions.AlertRulesRead,
                
                // Scraper permissions
                Permissions.ScrapersViewLogs,
                
                // User permissions
                Permissions.UsersRead
            }
        };
    }

    private static string GetPermissionDescription(string permissionName)
    {
        return permissionName switch
        {
            Permissions.ProductsCreate => "Create new products",
            Permissions.ProductsRead => "View products",
            Permissions.ProductsUpdate => "Update existing products",
            Permissions.ProductsDelete => "Delete products",
            Permissions.ProductsManageCategories => "Manage product categories",
            
            Permissions.PriceHistoryRead => "View price history",
            Permissions.AlertRulesCreate => "Create price alert rules",
            Permissions.AlertRulesRead => "View price alert rules",
            Permissions.AlertRulesUpdate => "Update price alert rules",
            Permissions.AlertRulesDelete => "Delete price alert rules",
            
            Permissions.ScrapersConfigure => "Configure scraper settings",
            Permissions.ScrapersRun => "Run scrapers manually",
            Permissions.ScrapersViewLogs => "View scraper logs",
            Permissions.ScrapersManageSites => "Manage scraper site configurations",
            
            Permissions.UsersCreate => "Create new users",
            Permissions.UsersRead => "View users",
            Permissions.UsersUpdate => "Update user information",
            Permissions.UsersDelete => "Delete users",
            Permissions.UsersManageRoles => "Manage user roles and permissions",
            
            Permissions.SystemViewLogs => "View system logs",
            Permissions.SystemManageSettings => "Manage system settings",
            Permissions.SystemManagePermissions => "Manage permissions and roles",
            
            _ => $"Permission: {permissionName}"
        };
    }
}
