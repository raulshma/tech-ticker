using Microsoft.EntityFrameworkCore;
using TechTicker.UserService.Data;
using TechTicker.UserService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Exceptions;
using TechTicker.Shared.Models;
using TechTicker.Shared.Utilities;
using BCrypt.Net;

namespace TechTicker.UserService.Services
{
    /// <summary>
    /// Service for user management operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly ITokenService _tokenService;

        public UserService(
            UserDbContext context, 
            ILogger<UserService> logger,
            ITokenService tokenService)
        {
            _context = context;
            _logger = logger;
            _tokenService = tokenService;
        }

        public async Task<Result<UserResponse>> RegisterUserAsync(RegisterUserRequest request)
        {
            try
            {
                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return Result<UserResponse>.Failure(
                        "User with this email already exists",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign default user role if it exists
                await AssignDefaultRoleAsync(user.UserId);                _logger.LogInformation("User registered successfully with ID: {UserId}", user.UserId);

                var userResponse = MapToUserResponse(user);
                return Result<UserResponse>.Success(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user with email: {Email}", request.Email);
                return Result<UserResponse>.Failure(ex);
            }
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !user.IsActive)
                {
                    return Result<LoginResponse>.Failure(
                        "Invalid email or password",
                        ApplicationConstants.ErrorCodes.AuthenticationFailed);
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Result<LoginResponse>.Failure(
                        "Invalid email or password",
                        ApplicationConstants.ErrorCodes.AuthenticationFailed);
                }

                // Update last login time
                user.LastLoginAt = DateTimeOffset.UtcNow;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();                // Generate access token
                var token = await _tokenService.GenerateAccessTokenAsync(user);

                var userResponse = MapToUserResponse(user);
                var loginResponse = new LoginResponse
                {
                    AccessToken = token.AccessToken,
                    TokenType = token.TokenType,
                    ExpiresIn = token.ExpiresIn,
                    User = userResponse
                };

                _logger.LogInformation("User logged in successfully: {UserId}", user.UserId);

                return Result<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return Result<LoginResponse>.Failure(ex);
            }
        }

        public async Task<Result<UserResponse>> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return Result<UserResponse>.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var userResponse = MapToUserResponse(user);
                return Result<UserResponse>.Success(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", userId);
                return Result<UserResponse>.Failure(ex);
            }
        }

        public async Task<Result<UserResponse>> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return Result<UserResponse>.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var userResponse = MapToUserResponse(user);
                return Result<UserResponse>.Success(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with email: {Email}", email);
                return Result<UserResponse>.Failure(ex);
            }
        }

        public async Task<Result<PagedResponse<UserResponse>>> GetUsersAsync(UserQueryRequest query)
        {
            try
            {
                var queryable = _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    queryable = queryable.Where(u => 
                        u.Email.ToLower().Contains(searchTerm) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)));
                }

                if (query.IsActive.HasValue)
                {
                    queryable = queryable.Where(u => u.IsActive == query.IsActive.Value);
                }

                if (query.EmailConfirmed.HasValue)
                {
                    queryable = queryable.Where(u => u.EmailConfirmed == query.EmailConfirmed.Value);
                }

                // Get total count
                var totalCount = await queryable.CountAsync();

                // Apply pagination
                var skip = (query.PageNumber - 1) * query.PageSize;
                var users = await queryable
                    .OrderBy(u => u.Email)
                    .Skip(skip)
                    .Take(query.PageSize)
                    .ToListAsync();

                var userResponses = new List<UserResponse>();
                foreach (var user in users)
                {
                    var response = MapToUserResponse(user);
                    userResponses.Add(response);
                }

                var pagedResponse = PagedResponse<UserResponse>.SuccessResult(
                    userResponses,
                    query.PageNumber,
                    query.PageSize,
                    totalCount);

                return Result<PagedResponse<UserResponse>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return Result<PagedResponse<UserResponse>>.Failure(ex);
            }
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Result<UserResponse>.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Update user properties
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully with ID: {UserId}", userId);

                var userResponse = await GetUserByIdAsync(userId);
                return userResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", userId);
                return Result<UserResponse>.Failure(ex);
            }
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Result.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return Result.Failure(
                        "Current password is incorrect",
                        ApplicationConstants.ErrorCodes.AuthenticationFailed);
                }

                // Hash new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return Result.Failure(ex);
            }
        }

        public async Task<Result> DeactivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Result.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                user.IsActive = false;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated successfully: {UserId}", userId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                return Result.Failure(ex);
            }
        }

        public async Task<Result> ActivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Result.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                user.IsActive = true;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User activated successfully: {UserId}", userId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", userId);
                return Result.Failure(ex);
            }
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.UserId == userId);
        }

        public async Task<Result<UserResponse>> ValidateUserAsync(string email, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null || !user.IsActive)
                {
                    return Result<UserResponse>.Failure(
                        "Invalid credentials",
                        ApplicationConstants.ErrorCodes.AuthenticationFailed);
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return Result<UserResponse>.Failure(
                        "Invalid credentials",
                        ApplicationConstants.ErrorCodes.AuthenticationFailed);
                }

                var userResponse = MapToUserResponse(user);
                return Result<UserResponse>.Success(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {Email}", email);
                return Result<UserResponse>.Failure(ex);
            }
        }

        public async Task<Result> AssignRoleToUserAsync(AssignRoleRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return Result.Failure(
                        "User not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
                if (role == null)
                {
                    return Result.Failure(
                        "Role not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.RoleId);

                if (existingUserRole != null)
                {
                    return Result.Failure(
                        "User already has this role",
                        ApplicationConstants.ErrorCodes.Conflict);
                }

                var userRole = new UserRole
                {
                    UserRoleId = Guid.NewGuid(),
                    UserId = request.UserId,
                    RoleId = role.RoleId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role {RoleName} assigned to user {UserId}", request.RoleName, request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", request.RoleName, request.UserId);
                return Result.Failure(ex);
            }
        }

        public async Task<Result> RemoveRoleFromUserAsync(AssignRoleRequest request)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
                if (role == null)
                {
                    return Result.Failure(
                        "Role not found",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.RoleId);

                if (userRole == null)
                {
                    return Result.Failure(
                        "User does not have this role",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role {RoleName} removed from user {UserId}", request.RoleName, request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", request.RoleName, request.UserId);
                return Result.Failure(ex);
            }
        }

        public async Task<Result<List<string>>> GetUserPermissionsAsync(Guid userId)
        {
            try
            {
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Name)
                    .Distinct()
                    .ToListAsync();

                return Result<List<string>>.Success(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for user: {UserId}", userId);
                return Result<List<string>>.Failure(ex);
            }
        }

        public async Task<Result<List<string>>> GetUserRolesAsync(Guid userId)
        {
            try
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return Result<List<string>>.Success(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user: {UserId}", userId);
                return Result<List<string>>.Failure(ex);
            }
        }

        private UserResponse MapToUserResponse(User user)
        {
            var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();

            return new UserResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                Roles = roles,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private async Task AssignDefaultRoleAsync(Guid userId)
        {
            try
            {
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (defaultRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserRoleId = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = defaultRole.RoleId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to assign default role to user {UserId}", userId);
                // Don't fail user registration if default role assignment fails
            }
        }
    }
}
