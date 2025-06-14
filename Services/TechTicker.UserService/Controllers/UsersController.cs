using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TechTicker.UserService.DTOs;
using TechTicker.UserService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;
using System.Security.Claims;

namespace TechTicker.UserService.Controllers
{
    /// <summary>
    /// Controller for user management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var result = await _userService.RegisterUserAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// User login
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _userService.GetUserByIdAsync(userId);
            return HandleResult(result);
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _userService.UpdateUserAsync(userId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Change current user password
        /// </summary>
        [HttpPost("me/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _userService.ChangePasswordAsync(userId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{userId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get paginated list of users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryRequest query)
        {
            var result = await _userService.GetUsersAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        [HttpPut("{userId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
        {
            var result = await _userService.UpdateUserAsync(userId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        [HttpPost("{userId:guid}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(Guid userId)
        {
            var result = await _userService.DeactivateUserAsync(userId);
            return HandleResult(result);
        }

        /// <summary>
        /// Assign role to user (Admin only)
        /// </summary>
        [HttpPost("roles/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            var result = await _userService.AssignRoleToUserAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Remove role from user (Admin only)
        /// </summary>
        [HttpPost("roles/remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
        {
            var result = await _userService.RemoveRoleFromUserAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user permissions
        /// </summary>
        [HttpGet("{userId:guid}/permissions")]
        [Authorize]
        public async Task<IActionResult> GetUserPermissions(Guid userId)
        {
            // Allow users to get their own permissions, or admins to get anyone's
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && (!Guid.TryParse(currentUserIdClaim, out var currentUserId) || currentUserId != userId))
            {
                return Forbid();
            }

            var result = await _userService.GetUserPermissionsAsync(userId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user roles
        /// </summary>
        [HttpGet("{userId:guid}/roles")]
        [Authorize]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            // Allow users to get their own roles, or admins to get anyone's
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && (!Guid.TryParse(currentUserIdClaim, out var currentUserId) || currentUserId != userId))
            {
                return Forbid();
            }

            var result = await _userService.GetUserRolesAsync(userId);
            return HandleResult(result);
        }
    }
}
