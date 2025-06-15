using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Admin operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IAlertRuleService _alertRuleService;

    public AdminController(
        IUserService userService,
        IAlertRuleService alertRuleService)
    {
        _userService = userService;
        _alertRuleService = alertRuleService;
    }

    /// <summary>
    /// Get all users (paginated)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetAllUsersAsync(page, pageSize);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid userId)
    {
        var result = await _userService.GetUserByIdAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="createDto">User creation data</param>
    /// <returns>Created user</returns>
    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createDto)
    {
        var result = await _userService.CreateUserAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateDto">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid userId, [FromBody] UpdateUserDto updateDto)
    {
        var result = await _userService.UpdateUserAsync(userId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("users/{userId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(Guid userId)
    {
        var result = await _userService.DeleteUserAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all alert rules (paginated, with optional filters)
    /// </summary>
    /// <param name="userId">Optional user filter</param>
    /// <param name="productId">Optional product filter</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of alert rules</returns>
    [HttpGet("alerts")]
    public async Task<ActionResult<PagedResponse<AlertRuleDto>>> GetAllAlerts(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _alertRuleService.GetAllAlertRulesAsync(userId, productId, page, pageSize);
        return HandlePagedResult(result);
    }
}
