using System.ComponentModel.DataAnnotations;
using TechTicker.Shared.Utilities;
using TechTicker.Shared.Common;

namespace TechTicker.UserService.DTOs
{
    /// <summary>
    /// DTO for user registration
    /// </summary>
    public class RegisterUserRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO for user login
    /// </summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for login response
    /// </summary>
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public UserResponse User { get; set; } = null!;
    }

    /// <summary>
    /// DTO for user responses
    /// </summary>
    public class UserResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for updating user profile
    /// </summary>
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }
    }

    /// <summary>
    /// DTO for changing password
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for user query requests
    /// </summary>
    public class UserQueryRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public bool? EmailConfirmed { get; set; }
    }

    /// <summary>
    /// DTO for role assignment/removal
    /// </summary>
    public class AssignRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for role responses
    /// </summary>
    public class RoleResponse
    {
        public Guid RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating roles
    /// </summary>
    public class CreateRoleRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public List<string> Permissions { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating roles
    /// </summary>
    public class UpdateRoleRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public List<string> Permissions { get; set; } = new();
    }    /// <summary>
    /// DTO for permission responses
    /// </summary>
    public class PermissionResponse
    {
        public Guid PermissionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for token responses
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public UserResponse User { get; set; } = null!;
    }
}
