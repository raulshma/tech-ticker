using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for User operations
/// </summary>
public interface IUserService
{
    Task<Result<UserDto>> RegisterUserAsync(RegisterUserDto registerDto);
    Task<Result<LoginResponseDto>> LoginUserAsync(LoginUserDto loginDto);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId);
    
    // Admin operations
    Task<Result<PagedResponse<UserDto>>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId);
    Task<Result<UserDto>> CreateUserAsync(CreateUserDto createDto);
    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserDto updateDto);
    Task<Result> DeleteUserAsync(Guid userId);
}
