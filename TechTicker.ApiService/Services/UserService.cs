using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for User operations
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IMappingService _mappingService;
    private readonly IPermissionService _permissionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IMappingService mappingService,
        IPermissionService permissionService,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _mappingService = mappingService;
        _permissionService = permissionService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterUserAsync(RegisterUserDto registerDto)
    {
        try
        {
            var user = _mappingService.MapToEntity(registerDto);
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"Registration failed: {errors}", "REGISTRATION_FAILED");
            }

            // Add default User role
            await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mappingService.MapToDto(user, roles);

            _logger.LogInformation("User {UserId} registered successfully", user.Id);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user with email {Email}", registerDto.Email);
            return Result<UserDto>.Failure("An error occurred during registration.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<LoginResponseDto>> LoginUserAsync(LoginUserDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                return Result<LoginResponseDto>.Failure("Invalid email or password.", "INVALID_CREDENTIALS");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Result<LoginResponseDto>.Failure("Invalid email or password.", "INVALID_CREDENTIALS");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = await GenerateJwtTokenAsync(user, roles);

            var loginResponse = new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Result<LoginResponseDto>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", loginDto.Email);
            return Result<LoginResponseDto>.Failure("An error occurred during login.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.", "RESOURCE_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mappingService.MapToDto(user, roles);

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user {UserId}", userId);
            return Result<UserDto>.Failure("An error occurred while retrieving user information.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<PagedResponse<UserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = _userManager.Users.Count();
            var users = _userManager.Users
                .OrderBy(u => u.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(_mappingService.MapToDto(user, roles));
            }

            var pagedResponse = PagedResponse<UserDto>.SuccessResult(
                userDtos, pageNumber, pageSize, totalCount);

            return Result<PagedResponse<UserDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return Result<PagedResponse<UserDto>>.Failure("An error occurred while retrieving users.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.", "RESOURCE_NOT_FOUND");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mappingService.MapToDto(user, roles);

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return Result<UserDto>.Failure("An error occurred while retrieving the user.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserDto createDto)
    {
        try
        {
            var user = _mappingService.MapToEntity(createDto);
            var result = await _userManager.CreateAsync(user, createDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"User creation failed: {errors}", "USER_CREATION_FAILED");
            }

            // Add specified roles
            foreach (var role in createDto.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mappingService.MapToDto(user, roles);

            _logger.LogInformation("Admin created user {UserId}", user.Id);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", createDto.Email);
            return Result<UserDto>.Failure("An error occurred while creating the user.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserDto updateDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.", "RESOURCE_NOT_FOUND");
            }

            _mappingService.MapToEntity(updateDto, user);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"User update failed: {errors}", "USER_UPDATE_FAILED");
            }

            // Update roles if specified
            if (updateDto.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                foreach (var role in updateDto.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mappingService.MapToDto(user, roles);

            _logger.LogInformation("Updated user {UserId}", userId);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return Result<UserDto>.Failure("An error occurred while updating the user.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found.", "RESOURCE_NOT_FOUND");
            }

            // Soft delete by setting IsActive to false
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Soft deleted user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Result.Failure("An error occurred while deleting the user.", "INTERNAL_ERROR");
        }
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add permission claims
        var userPermissionsResult = await _permissionService.GetUserPermissionsAsync(user.Id);
        if (userPermissionsResult.IsSuccess && userPermissionsResult.Data != null)
        {
            var permissions = userPermissionsResult.Data.Select(p => p.Name);
            claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
