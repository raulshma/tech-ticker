using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Permission operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PermissionController : BaseApiController
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    /// <returns>List of all permissions</returns>
    [HttpGet]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetAllPermissions()
    {
        var result = await _permissionService.GetAllPermissionsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get permissions by category
    /// </summary>
    /// <param name="category">Permission category</param>
    /// <returns>List of permissions in the specified category</returns>
    [HttpGet("category/{category}")]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetPermissionsByCategory(string category)
    {
        var result = await _permissionService.GetPermissionsByCategoryAsync(category);
        return HandleResult(result);
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Permission details</returns>
    [HttpGet("{id}")]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> GetPermissionById(Guid id)
    {
        var result = await _permissionService.GetPermissionByIdAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    /// <param name="createDto">Permission creation data</param>
    /// <returns>Created permission</returns>
    [HttpPost]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> CreatePermission([FromBody] CreatePermissionDto createDto)
    {
        var result = await _permissionService.CreatePermissionAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="updateDto">Permission update data</param>
    /// <returns>Updated permission</returns>
    [HttpPut("{id}")]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> UpdatePermission(Guid id, [FromBody] UpdatePermissionDto updateDto)
    {
        var result = await _permissionService.UpdatePermissionAsync(id, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePermission(Guid id)
    {
        var result = await _permissionService.DeletePermissionAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Get permissions assigned to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>List of permissions assigned to the role</returns>
    [HttpGet("role/{roleId}")]
    [RequirePermission(Permissions.UsersManageRoles)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetRolePermissions(Guid roleId)
    {
        var result = await _permissionService.GetRolePermissionsAsync(roleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get permissions assigned to a role by role name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>List of permissions assigned to the role</returns>
    [HttpGet("role/name/{roleName}")]
    [RequirePermission(Permissions.UsersManageRoles)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetRolePermissionsByName(string roleName)
    {
        var result = await _permissionService.GetRolePermissionsAsync(roleName);
        return HandleResult(result);
    }

    /// <summary>
    /// Assign a permission to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>Success status</returns>
    [HttpPost("role/{roleId}/permission/{permissionId}")]
    [RequirePermission(Permissions.UsersManageRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> AssignPermissionToRole(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove a permission from a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("role/{roleId}/permission/{permissionId}")]
    [RequirePermission(Permissions.UsersManageRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> RemovePermissionFromRole(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all permissions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of permissions for the user</returns>
    [HttpGet("user/{userId}")]
    [RequirePermission(Permissions.UsersRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetUserPermissions(Guid userId)
    {
        var result = await _permissionService.GetUserPermissionsAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Check if a user has a specific permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>True if user has the permission</returns>
    [HttpGet("user/{userId}/check/{permissionName}")]
    [RequirePermission(Permissions.UsersRead)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckUserPermission(Guid userId, string permissionName)
    {
        var result = await _permissionService.UserHasPermissionAsync(userId, permissionName);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk assign permissions to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionIds">List of permission IDs to assign</param>
    /// <returns>Success status</returns>
    [HttpPost("role/{roleId}/permissions/bulk")]
    [RequirePermission(Permissions.UsersManageRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> BulkAssignPermissionsToRole(Guid roleId, [FromBody] IEnumerable<Guid> permissionIds)
    {
        var result = await _permissionService.BulkAssignPermissionsToRoleAsync(roleId, permissionIds);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all available permission categories
    /// </summary>
    /// <returns>List of permission categories with their permissions</returns>
    [HttpGet("categories")]
    [RequirePermission(Permissions.SystemManagePermissions)]
    public ActionResult<ApiResponse<Dictionary<string, string[]>>> GetPermissionCategories()
    {
        var categories = Permissions.GetPermissionsByCategory();
        return HandleResult(Result<Dictionary<string, string[]>>.Success(categories));
    }
}
