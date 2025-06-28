using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.DataAccess;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Constants;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class PermissionServiceTests : IDisposable
{
    private readonly TechTickerDbContext _context;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<PermissionService>> _loggerMock;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TechTickerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TechTickerDbContext(options);

        _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
            Mock.Of<IRoleStore<IdentityRole<Guid>>>(), null!, null!, null!, null!);

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<PermissionService>>();

        _permissionService = new PermissionService(
            _context,
            _roleManagerMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePermissionAsync_ShouldCreatePermission_WhenValidData()
    {
        // Arrange
        var createDto = new CreatePermissionDto
        {
            Name = "Test.Permission",
            Description = "Test permission",
            Category = "Test"
        };

        // Act
        var result = await _permissionService.CreatePermissionAsync(createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(createDto.Name, result.Data.Name);
        Assert.Equal(createDto.Description, result.Data.Description);
        Assert.Equal(createDto.Category, result.Data.Category);

        var permissionInDb = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == createDto.Name);
        Assert.NotNull(permissionInDb);
    }

    [Fact]
    public async Task CreatePermissionAsync_ShouldFail_WhenPermissionAlreadyExists()
    {
        // Arrange
        var existingPermission = new Permission
        {
            PermissionId = Guid.NewGuid(),
            Name = "Existing.Permission",
            Description = "Existing permission",
            Category = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Permissions.Add(existingPermission);
        await _context.SaveChangesAsync();

        var createDto = new CreatePermissionDto
        {
            Name = "Existing.Permission",
            Description = "Duplicate permission",
            Category = "Test"
        };

        // Act
        var result = await _permissionService.CreatePermissionAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PERMISSION_ALREADY_EXISTS", result.ErrorCode);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnAllPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = "Permission1",
                Description = "First permission",
                Category = "Category1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = "Permission2",
                Description = "Second permission",
                Category = "Category2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.GetAllPermissionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_ShouldAssignPermission_WhenValidData()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var role = new IdentityRole<Guid> { Id = roleId, Name = "TestRole" };
        var permission = new Permission
        {
            PermissionId = permissionId,
            Name = "Test.Permission",
            Description = "Test permission",
            Category = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        _roleManagerMock.Setup(rm => rm.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        // Act
        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);

        // Assert
        Assert.True(result.IsSuccess);

        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        Assert.NotNull(rolePermission);
    }

    [Fact]
    public async Task GetPermissionsByCategoryAsync_ShouldReturnPermissionsInCategory()
    {
        // Arrange
        var category = "TestCategory";
        var permissions = new[]
        {
            new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = "Permission1",
                Description = "First permission",
                Category = category,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = "Permission2",
                Description = "Second permission",
                Category = "OtherCategory",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.GetPermissionsByCategoryAsync(category);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Permission1", result.Data.First().Name);
    }

    [Fact]
    public void PermissionsConstants_ShouldHaveAllRequiredPermissions()
    {
        // Arrange & Act
        var allPermissions = Permissions.GetAllPermissions();
        var permissionsByCategory = Permissions.GetPermissionsByCategory();

        // Assert
        Assert.NotEmpty(allPermissions);
        Assert.NotEmpty(permissionsByCategory);

        // Verify key permissions exist
        Assert.Contains(Permissions.ProductsCreate, allPermissions);
        Assert.Contains(Permissions.ProductsRead, allPermissions);
        Assert.Contains(Permissions.ProductsUpdate, allPermissions);
        Assert.Contains(Permissions.ProductsDelete, allPermissions);
        Assert.Contains(Permissions.UsersManageRoles, allPermissions);
        Assert.Contains(Permissions.SystemManagePermissions, allPermissions);

        // Verify categories exist
        Assert.True(permissionsByCategory.ContainsKey("Products"));
        Assert.True(permissionsByCategory.ContainsKey("Users"));
        Assert.True(permissionsByCategory.ContainsKey("System"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
