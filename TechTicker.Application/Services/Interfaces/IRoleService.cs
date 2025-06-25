using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Role operations
/// </summary>
public interface IRoleService
{
    Task<Result<IEnumerable<RoleInfoDto>>> GetAllRolesAsync();
    Task<Result<RoleInfoDto>> GetRoleByIdAsync(Guid roleId);
    Task<Result<RoleInfoDto>> CreateRoleAsync(string roleName);
    Task<Result> DeleteRoleAsync(Guid roleId);
    Task<Result<IEnumerable<string>>> GetUserRolesAsync(Guid userId);
    Task<Result> AssignRoleToUserAsync(Guid userId, string roleName);
    Task<Result> RemoveRoleFromUserAsync(Guid userId, string roleName);
    Task<Result<IEnumerable<UserRoleInfoDto>>> GetUsersInRoleAsync(string roleName);
}

/// <summary>
/// DTO for Role information
/// </summary>
public class RoleInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int UserCount { get; set; }
    public bool IsSystemRole { get; set; }
}

/// <summary>
/// DTO for User-Role information
/// </summary>
public class UserRoleInfoDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public DateTimeOffset AssignedAt { get; set; }
}
