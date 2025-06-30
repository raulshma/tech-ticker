using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.Services;
using TechTicker.DataAccess;
using TechTicker.Domain.Entities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class RoleServiceTests : IDisposable
{
    private readonly TechTickerDbContext _context;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<RoleService>> _loggerMock;
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<TechTickerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TechTickerDbContext(options);

        // Mock RoleManager
        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null, null, null, null);

        // Mock UserManager
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _loggerMock = new Mock<ILogger<RoleService>>();

        _service = new RoleService(
            _roleManagerMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldHandleException_WhenDatabaseErrorOccurs()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.Roles)
            .Throws(new Exception("Database error"));

        // Act
        var result = await _service.GetAllRolesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INTERNAL_ERROR", result.ErrorCode);
        Assert.Equal("An error occurred while retrieving roles.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ShouldReturnRole_WhenRoleExists()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new IdentityRole<Guid> { Id = roleId, Name = "Admin" };
        var users = new List<ApplicationUser> { new ApplicationUser { Id = Guid.NewGuid() } };

        _roleManagerMock.Setup(r => r.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetRoleByIdAsync(roleId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleId, result.Data.Id);
        Assert.Equal("Admin", result.Data.Name);
        Assert.Equal("Administrator", result.Data.DisplayName);
        Assert.Equal(1, result.Data.UserCount);
        Assert.True(result.Data.IsSystemRole);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ShouldReturnFailure_WhenRoleNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleManagerMock.Setup(r => r.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync((IdentityRole<Guid>?)null);

        // Act
        var result = await _service.GetRoleByIdAsync(roleId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_FOUND", result.ErrorCode);
        Assert.Equal("Role not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateRole_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleName = "Moderator";
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName, result.Data.Name);
        Assert.Equal("Moderator", result.Data.DisplayName);
        Assert.Equal(0, result.Data.UserCount);
        Assert.False(result.Data.IsSystemRole);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnFailure_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleName = "Admin";
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateRoleAsync(roleName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_EXISTS", result.ErrorCode);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnFailure_WhenCreationFails()
    {
        // Arrange
        var roleName = "TestRole";
        var identityError = new IdentityError { Description = "Creation failed" };
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _service.CreateRoleAsync(roleName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_CREATION_FAILED", result.ErrorCode);
        Assert.Contains("Creation failed", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldDeleteRole_WhenRoleExistsAndNotSystemRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new IdentityRole<Guid> { Id = roleId, Name = "Moderator" };

        _roleManagerMock.Setup(r => r.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);
        _roleManagerMock.Setup(r => r.DeleteAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.DeleteRoleAsync(roleId);

        // Assert
        Assert.True(result.IsSuccess);
        _roleManagerMock.Verify(r => r.DeleteAsync(role), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnFailure_WhenRoleNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleManagerMock.Setup(r => r.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync((IdentityRole<Guid>?)null);

        // Act
        var result = await _service.DeleteRoleAsync(roleId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnFailure_WhenTryingToDeleteSystemRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new IdentityRole<Guid> { Id = roleId, Name = "Admin" };

        _roleManagerMock.Setup(r => r.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        // Act
        var result = await _service.DeleteRoleAsync(roleId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SYSTEM_ROLE_DELETE_FORBIDDEN", result.ErrorCode);
        Assert.Contains("Cannot delete system roles", result.ErrorMessage);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnUserRoles_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId };
        var roles = new List<string> { "Admin", "User" };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _service.GetUserRolesAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count());
        Assert.Contains("Admin", result.Data);
        Assert.Contains("User", result.Data);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.GetUserRolesAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_ShouldAssignRole_WhenUserAndRoleExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "Admin";
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, roleName))
            .ReturnsAsync(false);
        _userManagerMock.Setup(u => u.AddToRoleAsync(user, roleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.AssignRoleToUserAsync(userId, roleName);

        // Assert
        Assert.True(result.IsSuccess);
        _userManagerMock.Verify(u => u.AddToRoleAsync(user, roleName), Times.Once);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "Admin";
        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.AssignRoleToUserAsync(userId, roleName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_ShouldReturnFailure_WhenRoleNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "NonExistentRole";
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AssignRoleToUserAsync(userId, roleName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_ShouldRemoveRole_WhenUserHasRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "Admin";
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, roleName))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.RemoveFromRoleAsync(user, roleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.RemoveRoleFromUserAsync(userId, roleName);

        // Assert
        Assert.True(result.IsSuccess);
        _userManagerMock.Verify(u => u.RemoveFromRoleAsync(user, roleName), Times.Once);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_ShouldReturnUsers_WhenRoleExists()
    {
        // Arrange
        var roleName = "Admin";
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin1@test.com", Email = "admin1@test.com" },
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin2@test.com", Email = "admin2@test.com" }
        };

        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(roleName))
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetUsersInRoleAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count());
        Assert.All(result.Data, userInfo => Assert.Contains("@test.com", userInfo.Email));
    }

    [Fact]
    public async Task GetUsersInRoleAsync_ShouldReturnFailure_WhenRoleNotFound()
    {
        // Arrange
        var roleName = "NonExistentRole";
        _roleManagerMock.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetUsersInRoleAsync(roleName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.Roles)
            .Throws(new Exception("Database error"));

        // Act
        var result = await _service.GetAllRolesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INTERNAL_ERROR", result.ErrorCode);
        Assert.Contains("error occurred while retrieving roles", result.ErrorMessage);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
} 