using TechTicker.Application.DTOs;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing permissions and role-permission mappings
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permissions
    /// </summary>
    Task<Result<IEnumerable<PermissionDto>>> GetAllPermissionsAsync();

    /// <summary>
    /// Gets permissions by category
    /// </summary>
    Task<Result<IEnumerable<PermissionDto>>> GetPermissionsByCategoryAsync(string category);

    /// <summary>
    /// Gets a permission by ID
    /// </summary>
    Task<Result<PermissionDto>> GetPermissionByIdAsync(Guid permissionId);

    /// <summary>
    /// Creates a new permission
    /// </summary>
    Task<Result<PermissionDto>> CreatePermissionAsync(CreatePermissionDto createDto);

    /// <summary>
    /// Updates an existing permission
    /// </summary>
    Task<Result<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto updateDto);

    /// <summary>
    /// Deletes a permission
    /// </summary>
    Task<Result<bool>> DeletePermissionAsync(Guid permissionId);

    /// <summary>
    /// Gets all permissions assigned to a role
    /// </summary>
    Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(Guid roleId);

    /// <summary>
    /// Gets all permissions assigned to a role by role name
    /// </summary>
    Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Assigns a permission to a role
    /// </summary>
    Task<Result<bool>> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);

    /// <summary>
    /// Assigns a permission to a role by names
    /// </summary>
    Task<Result<bool>> AssignPermissionToRoleAsync(string roleName, string permissionName);

    /// <summary>
    /// Removes a permission from a role
    /// </summary>
    Task<Result<bool>> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

    /// <summary>
    /// Removes a permission from a role by names
    /// </summary>
    Task<Result<bool>> RemovePermissionFromRoleAsync(string roleName, string permissionName);

    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    Task<Result<bool>> UserHasPermissionAsync(Guid userId, string permissionName);

    /// <summary>
    /// Gets all permissions for a user (through their roles)
    /// </summary>
    Task<Result<IEnumerable<PermissionDto>>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Bulk assigns permissions to a role
    /// </summary>
    Task<Result<bool>> BulkAssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds);

    /// <summary>
    /// Bulk assigns permissions to a role by names
    /// </summary>
    Task<Result<bool>> BulkAssignPermissionsToRoleAsync(string roleName, IEnumerable<string> permissionNames);
}
