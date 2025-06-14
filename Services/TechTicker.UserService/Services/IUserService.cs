using TechTicker.UserService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.UserService.Services
{
    /// <summary>
    /// Interface for user management operations
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        Task<Result<UserResponse>> RegisterUserAsync(RegisterUserRequest request);

        /// <summary>
        /// Authenticates a user and returns login information
        /// </summary>
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        Task<Result<UserResponse>> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Gets a user by email
        /// </summary>
        Task<Result<UserResponse>> GetUserByEmailAsync(string email);

        /// <summary>
        /// Gets a paginated list of users
        /// </summary>
        Task<Result<PagedResponse<UserResponse>>> GetUsersAsync(UserQueryRequest query);

        /// <summary>
        /// Updates a user's profile
        /// </summary>
        Task<Result<UserResponse>> UpdateUserAsync(Guid userId, UpdateUserRequest request);

        /// <summary>
        /// Changes a user's password
        /// </summary>
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        /// <summary>
        /// Deactivates a user account
        /// </summary>
        Task<Result> DeactivateUserAsync(Guid userId);

        /// <summary>
        /// Activates a user account
        /// </summary>
        Task<Result> ActivateUserAsync(Guid userId);

        /// <summary>
        /// Checks if a user exists by ID
        /// </summary>
        Task<bool> UserExistsAsync(Guid userId);

        /// <summary>
        /// Validates user credentials (for internal service calls)
        /// </summary>
        Task<Result<UserResponse>> ValidateUserAsync(string email, string password);

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        Task<Result> AssignRoleToUserAsync(AssignRoleRequest request);

        /// <summary>
        /// Removes a role from a user
        /// </summary>
        Task<Result> RemoveRoleFromUserAsync(AssignRoleRequest request);

        /// <summary>
        /// Gets user permissions
        /// </summary>
        Task<Result<List<string>>> GetUserPermissionsAsync(Guid userId);

        /// <summary>
        /// Gets user roles
        /// </summary>
        Task<Result<List<string>>> GetUserRolesAsync(Guid userId);
    }
}
