using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service implementation for Role operations
/// </summary>
public class RoleService(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<RoleService> logger) : IRoleService
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<RoleService> _logger = logger;

    // System roles that cannot be deleted
    private readonly string[] _systemRoles = ["Admin", "User"];

    // Role definitions with display names and descriptions
    private readonly Dictionary<string, (string DisplayName, string Description)> _roleDefinitions = new()
    {
        { "Admin", ("Administrator", "Full system access with all administrative privileges") },
        { "User", ("User", "Standard user with basic access to user features") },
        { "Moderator", ("Moderator", "Can manage content and moderate user activities") }
    };

    public async Task<Result<IEnumerable<RoleInfoDto>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleInfos = new List<RoleInfoDto>();

            foreach (var role in roles)
            {
                var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
                var (displayName, description) = _roleDefinitions.TryGetValue(role.Name!, out var def) 
                    ? def 
                    : (role.Name!, "Custom role");

                roleInfos.Add(new RoleInfoDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    DisplayName = displayName,
                    Description = description,
                    UserCount = userCount,
                    IsSystemRole = _systemRoles.Contains(role.Name!)
                });
            }

            return Result<IEnumerable<RoleInfoDto>>.Success(roleInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Result<IEnumerable<RoleInfoDto>>.Failure("An error occurred while retrieving roles.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<RoleInfoDto>> GetRoleByIdAsync(Guid roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return Result<RoleInfoDto>.Failure("Role not found.", "ROLE_NOT_FOUND");
            }

            var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
            var (displayName, description) = _roleDefinitions.TryGetValue(role.Name!, out var def) 
                ? def 
                : (role.Name!, "Custom role");

            var roleInfo = new RoleInfoDto
            {
                Id = role.Id,
                Name = role.Name!,
                DisplayName = displayName,
                Description = description,
                UserCount = userCount,
                IsSystemRole = _systemRoles.Contains(role.Name!)
            };

            return Result<RoleInfoDto>.Success(roleInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", roleId);
            return Result<RoleInfoDto>.Failure("An error occurred while retrieving the role.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<RoleInfoDto>> CreateRoleAsync(string roleName)
    {
        try
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return Result<RoleInfoDto>.Failure($"Role '{roleName}' already exists.", "ROLE_EXISTS");
            }

            var role = new IdentityRole<Guid>
            {
                Name = roleName
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<RoleInfoDto>.Failure($"Failed to create role: {errors}", "ROLE_CREATION_FAILED");
            }

            var (displayName, description) = _roleDefinitions.TryGetValue(roleName, out var def) 
                ? def 
                : (roleName, "Custom role");

            var roleInfo = new RoleInfoDto
            {
                Id = role.Id,
                Name = role.Name!,
                DisplayName = displayName,
                Description = description,
                UserCount = 0,
                IsSystemRole = _systemRoles.Contains(roleName)
            };

            _logger.LogInformation("Role {RoleName} created successfully", roleName);
            return Result<RoleInfoDto>.Success(roleInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", roleName);
            return Result<RoleInfoDto>.Failure("An error occurred while creating the role.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteRoleAsync(Guid roleId)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                return Result.Failure("Role not found.", "ROLE_NOT_FOUND");
            }

            if (_systemRoles.Contains(role.Name!))
            {
                return Result.Failure("Cannot delete system roles.", "SYSTEM_ROLE_DELETE_FORBIDDEN");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to delete role: {errors}", "ROLE_DELETION_FAILED");
            }

            _logger.LogInformation("Role {RoleName} deleted successfully", role.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return Result.Failure("An error occurred while deleting the role.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<IEnumerable<string>>.Failure("User not found.", "USER_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Result<IEnumerable<string>>.Success(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("An error occurred while retrieving user roles.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> AssignRoleToUserAsync(Guid userId, string roleName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found.", "USER_NOT_FOUND");
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Result.Failure($"Role '{roleName}' does not exist.", "ROLE_NOT_FOUND");
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return Result.Failure($"User already has role '{roleName}'.", "USER_ALREADY_IN_ROLE");
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to assign role: {errors}", "ROLE_ASSIGNMENT_FAILED");
            }

            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
            return Result.Failure("An error occurred while assigning the role.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> RemoveRoleFromUserAsync(Guid userId, string roleName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found.", "USER_NOT_FOUND");
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return Result.Failure($"User does not have role '{roleName}'.", "USER_NOT_IN_ROLE");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to remove role: {errors}", "ROLE_REMOVAL_FAILED");
            }

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            return Result.Failure("An error occurred while removing the role.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<UserRoleInfoDto>>> GetUsersInRoleAsync(string roleName)
    {
        try
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Result<IEnumerable<UserRoleInfoDto>>.Failure($"Role '{roleName}' does not exist.", "ROLE_NOT_FOUND");
            }

            var users = await _userManager.GetUsersInRoleAsync(roleName);
            var userRoleInfos = users.Select(u => new UserRoleInfoDto
            {
                UserId = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                RoleName = roleName,
                AssignedAt = u.CreatedAt // This is a simplification; in a real system, you'd track role assignment dates
            }).ToList();

            return Result<IEnumerable<UserRoleInfoDto>>.Success(userRoleInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users in role {RoleName}", roleName);
            return Result<IEnumerable<UserRoleInfoDto>>.Failure("An error occurred while retrieving users in role.", "INTERNAL_ERROR");
        }
    }
}
