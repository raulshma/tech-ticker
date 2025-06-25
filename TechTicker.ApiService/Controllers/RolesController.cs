using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Role management operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RolesController(IRoleService roleService) : BaseApiController
{
    private readonly IRoleService _roleService = roleService;

    /// <summary>
    /// Get all available roles
    /// </summary>
    /// <returns>List of roles</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleInfoDto>>>> GetAllRoles()
    {
        var result = await _roleService.GetAllRolesAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>Role details</returns>
    [HttpGet("{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<RoleInfoDto>>> GetRole(Guid roleId)
    {
        var result = await _roleService.GetRoleByIdAsync(roleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <param name="createRoleDto">Role creation data</param>
    /// <returns>Created role</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleInfoDto>>> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        var result = await _roleService.CreateRoleAsync(createRoleDto.Name);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{roleId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteRole(Guid roleId)
    {
        var result = await _roleService.DeleteRoleAsync(roleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get users in a specific role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>List of users in the role</returns>
    [HttpGet("{roleName}/users")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserRoleInfoDto>>>> GetUsersInRole(string roleName)
    {
        var result = await _roleService.GetUsersInRoleAsync(roleName);
        return HandleResult(result);
    }
}

/// <summary>
/// DTO for creating a new role
/// </summary>
public class CreateRoleDto
{
    public string Name { get; set; } = null!;
}
