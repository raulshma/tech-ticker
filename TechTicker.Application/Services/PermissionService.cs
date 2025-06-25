using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing permissions and role-permission mappings
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly TechTickerDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        TechTickerDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var permissionDtos = permissions.Select(MapToDto);
            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all permissions");
            return Result<IEnumerable<PermissionDto>>.Failure("Failed to retrieve permissions", "PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetPermissionsByCategoryAsync(string category)
    {
        try
        {
            var permissions = await _context.Permissions
                .Where(p => p.Category == category)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var permissionDtos = permissions.Select(MapToDto);
            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for category {Category}", category);
            return Result<IEnumerable<PermissionDto>>.Failure($"Failed to retrieve permissions for category {category}", "PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<PermissionDto>> GetPermissionByIdAsync(Guid permissionId)
    {
        try
        {
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return Result<PermissionDto>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            return Result<PermissionDto>.Success(MapToDto(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission {PermissionId}", permissionId);
            return Result<PermissionDto>.Failure("Failed to retrieve permission", "PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<PermissionDto>> CreatePermissionAsync(CreatePermissionDto createDto)
    {
        try
        {
            // Check if permission already exists
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == createDto.Name);

            if (existingPermission != null)
            {
                return Result<PermissionDto>.Failure("Permission with this name already exists", "PERMISSION_ALREADY_EXISTS");
            }

            var permission = new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Category = createDto.Category
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created permission {PermissionName}", permission.Name);
            return Result<PermissionDto>.Success(MapToDto(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {PermissionName}", createDto.Name);
            return Result<PermissionDto>.Failure("Failed to create permission", "PERMISSION_CREATION_ERROR");
        }
    }

    public async Task<Result<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto updateDto)
    {
        try
        {
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return Result<PermissionDto>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            if (!string.IsNullOrEmpty(updateDto.Description))
                permission.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.Category))
                permission.Category = updateDto.Category;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated permission {PermissionName}", permission.Name);
            return Result<PermissionDto>.Success(MapToDto(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", permissionId);
            return Result<PermissionDto>.Failure("Failed to update permission", "PERMISSION_UPDATE_ERROR");
        }
    }

    public async Task<Result<bool>> DeletePermissionAsync(Guid permissionId)
    {
        try
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null)
            {
                return Result<bool>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            // Remove all role-permission mappings first
            _context.RolePermissions.RemoveRange(permission.RolePermissions);
            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted permission {PermissionName}", permission.Name);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", permissionId);
            return Result<bool>.Failure("Failed to delete permission", "PERMISSION_DELETION_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return Result<IEnumerable<PermissionDto>>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var permissionDtos = permissions.Select(MapToDto);
            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role {RoleId}", roleId);
            return Result<IEnumerable<PermissionDto>>.Failure("Failed to retrieve role permissions", "ROLE_PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleName)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return Result<IEnumerable<PermissionDto>>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            return await GetRolePermissionsAsync(role.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role {RoleName}", roleName);
            return Result<IEnumerable<PermissionDto>>.Failure("Failed to retrieve role permissions", "ROLE_PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<bool>> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return Result<bool>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return Result<bool>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            // Check if assignment already exists
            var existingAssignment = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existingAssignment != null)
            {
                return Result<bool>.Failure("Permission already assigned to role", "PERMISSION_ALREADY_ASSIGNED");
            }

            var rolePermission = new RolePermission
            {
                RolePermissionId = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned permission {PermissionName} to role {RoleName}", permission.Name, role.Name);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return Result<bool>.Failure("Failed to assign permission to role", "PERMISSION_ASSIGNMENT_ERROR");
        }
    }

    public async Task<Result<bool>> AssignPermissionToRoleAsync(string roleName, string permissionName)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return Result<bool>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null)
            {
                return Result<bool>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            return await AssignPermissionToRoleAsync(role.Id, permission.PermissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionName} to role {RoleName}", permissionName, roleName);
            return Result<bool>.Failure("Failed to assign permission to role", "PERMISSION_ASSIGNMENT_ERROR");
        }
    }

    public async Task<Result<bool>> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return Result<bool>.Failure("Permission not assigned to role", "PERMISSION_NOT_ASSIGNED");
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return Result<bool>.Failure("Failed to remove permission from role", "PERMISSION_REMOVAL_ERROR");
        }
    }

    public async Task<Result<bool>> RemovePermissionFromRoleAsync(string roleName, string permissionName)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return Result<bool>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null)
            {
                return Result<bool>.Failure("Permission not found", "PERMISSION_NOT_FOUND");
            }

            return await RemovePermissionFromRoleAsync(role.Id, permission.PermissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionName} from role {RoleName}", permissionName, roleName);
            return Result<bool>.Failure("Failed to remove permission from role", "PERMISSION_REMOVAL_ERROR");
        }
    }

    public async Task<Result<bool>> UserHasPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<bool>.Failure("User not found", "USER_NOT_FOUND");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
            {
                return Result<bool>.Success(false);
            }

            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .AnyAsync(rp => userRoles.Contains(rp.Role.Name!) && rp.Permission.Name == permissionName);

            return Result<bool>.Success(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionName} for user {UserId}", permissionName, userId);
            return Result<bool>.Failure("Failed to check user permission", "PERMISSION_CHECK_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<IEnumerable<PermissionDto>>.Failure("User not found", "USER_NOT_FOUND");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
            {
                return Result<IEnumerable<PermissionDto>>.Success(Enumerable.Empty<PermissionDto>());
            }

            var permissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => userRoles.Contains(rp.Role.Name!))
                .Select(rp => rp.Permission)
                .Distinct()
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var permissionDtos = permissions.Select(MapToDto);
            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user {UserId}", userId);
            return Result<IEnumerable<PermissionDto>>.Failure("Failed to retrieve user permissions", "USER_PERMISSION_RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<bool>> BulkAssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return Result<bool>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permissionIdsList = permissionIds.ToList();
            var existingPermissions = await _context.Permissions
                .Where(p => permissionIdsList.Contains(p.PermissionId))
                .ToListAsync();

            if (existingPermissions.Count != permissionIdsList.Count)
            {
                return Result<bool>.Failure("One or more permissions not found", "PERMISSION_NOT_FOUND");
            }

            // Get existing assignments to avoid duplicates
            var existingAssignments = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId && permissionIdsList.Contains(rp.PermissionId))
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var newAssignments = permissionIdsList
                .Except(existingAssignments)
                .Select(permissionId => new RolePermission
                {
                    RolePermissionId = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId
                });

            _context.RolePermissions.AddRange(newAssignments);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk assigned {Count} permissions to role {RoleName}", newAssignments.Count(), role.Name);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning permissions to role {RoleId}", roleId);
            return Result<bool>.Failure("Failed to bulk assign permissions to role", "BULK_PERMISSION_ASSIGNMENT_ERROR");
        }
    }

    public async Task<Result<bool>> BulkAssignPermissionsToRoleAsync(string roleName, IEnumerable<string> permissionNames)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return Result<bool>.Failure("Role not found", "ROLE_NOT_FOUND");
            }

            var permissionNamesList = permissionNames.ToList();
            var permissions = await _context.Permissions
                .Where(p => permissionNamesList.Contains(p.Name))
                .ToListAsync();

            if (permissions.Count != permissionNamesList.Count)
            {
                return Result<bool>.Failure("One or more permissions not found", "PERMISSION_NOT_FOUND");
            }

            var permissionIds = permissions.Select(p => p.PermissionId);
            return await BulkAssignPermissionsToRoleAsync(role.Id, permissionIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning permissions to role {RoleName}", roleName);
            return Result<bool>.Failure("Failed to bulk assign permissions to role", "BULK_PERMISSION_ASSIGNMENT_ERROR");
        }
    }

    private static PermissionDto MapToDto(Permission permission)
    {
        return new PermissionDto
        {
            PermissionId = permission.PermissionId,
            Name = permission.Name,
            Description = permission.Description,
            Category = permission.Category,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt
        };
    }
}
