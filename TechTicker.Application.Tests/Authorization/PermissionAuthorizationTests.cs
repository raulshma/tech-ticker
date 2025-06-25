using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Constants;
using Xunit;

namespace TechTicker.Application.Tests.Authorization;

public class PermissionAuthorizationTests
{
    [Fact]
    public void PermissionRequirement_ShouldStorePermissionName()
    {
        // Arrange
        var permissionName = "Test.Permission";

        // Act
        var requirement = new PermissionRequirement(permissionName);

        // Assert
        Assert.Equal(permissionName, requirement.Permission);
    }

    [Fact]
    public void PermissionRequirement_ShouldThrowException_WhenPermissionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PermissionRequirement(null!));
    }

    [Fact]
    public void RequirePermissionAttribute_ShouldSetCorrectPolicy()
    {
        // Arrange
        var permission = "Test.Permission";

        // Act
        var attribute = new RequirePermissionAttribute(permission);

        // Assert
        Assert.Equal($"Permission.{permission}", attribute.Policy);
    }

    [Theory]
    [InlineData("Products.Create")]
    [InlineData("Products.Read")]
    [InlineData("Users.ManageRoles")]
    [InlineData("System.ManagePermissions")]
    public void RequirePermissionAttribute_ShouldWorkWithRealPermissions(string permission)
    {
        // Act
        var attribute = new RequirePermissionAttribute(permission);

        // Assert
        Assert.Equal($"Permission.{permission}", attribute.Policy);
        Assert.NotNull(attribute.Policy);
        Assert.NotEmpty(attribute.Policy);
    }

    [Fact]
    public void PermissionConstants_ShouldHaveConsistentNaming()
    {
        // Arrange
        var allPermissions = Permissions.GetAllPermissions();

        // Act & Assert
        foreach (var permission in allPermissions)
        {
            // Should follow Category.Action pattern
            Assert.Contains(".", permission);
            
            var parts = permission.Split('.');
            Assert.Equal(2, parts.Length);
            
            // Category should not be empty
            Assert.NotEmpty(parts[0]);
            
            // Action should not be empty
            Assert.NotEmpty(parts[1]);
            
            // Should not contain spaces
            Assert.DoesNotContain(" ", permission);
            
            // Should be properly capitalized
            Assert.True(char.IsUpper(parts[0][0]), $"Category in '{permission}' should start with uppercase");
            Assert.True(char.IsUpper(parts[1][0]), $"Action in '{permission}' should start with uppercase");
        }
    }

    [Fact]
    public void PermissionCategories_ShouldHaveExpectedPermissions()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();

        // Act & Assert
        // Products category should have CRUD operations
        Assert.True(categories.ContainsKey("Products"));
        var productPermissions = categories["Products"];
        Assert.Contains("Products.Create", productPermissions);
        Assert.Contains("Products.Read", productPermissions);
        Assert.Contains("Products.Update", productPermissions);
        Assert.Contains("Products.Delete", productPermissions);
        Assert.Contains("Products.ManageCategories", productPermissions);

        // Users category should have user management
        Assert.True(categories.ContainsKey("Users"));
        var userPermissions = categories["Users"];
        Assert.Contains("Users.Create", userPermissions);
        Assert.Contains("Users.Read", userPermissions);
        Assert.Contains("Users.Update", userPermissions);
        Assert.Contains("Users.Delete", userPermissions);
        Assert.Contains("Users.ManageRoles", userPermissions);

        // System category should have admin functions
        Assert.True(categories.ContainsKey("System"));
        var systemPermissions = categories["System"];
        Assert.Contains("System.ViewLogs", systemPermissions);
        Assert.Contains("System.ManageSettings", systemPermissions);
        Assert.Contains("System.ManagePermissions", systemPermissions);
    }

    [Fact]
    public void AllPermissions_ShouldBeCategorized()
    {
        // Arrange
        var allPermissions = Permissions.GetAllPermissions();
        var categorizedPermissions = Permissions.GetPermissionsByCategory()
            .SelectMany(kvp => kvp.Value)
            .ToHashSet();

        // Act & Assert
        foreach (var permission in allPermissions)
        {
            Assert.Contains(permission, categorizedPermissions);
        }

        // Verify counts match
        Assert.Equal(allPermissions.Length, categorizedPermissions.Count);
    }

    [Fact]
    public void PermissionCategories_ShouldNotHaveDuplicates()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();

        // Act & Assert
        foreach (var (categoryName, permissions) in categories)
        {
            var uniquePermissions = permissions.Distinct().ToArray();
            Assert.Equal(permissions.Length, uniquePermissions.Length);
        }
    }

    [Fact]
    public void PermissionConstants_ShouldMatchExpectedValues()
    {
        // Act & Assert - Verify specific permission values
        Assert.Equal("Products.Create", Permissions.ProductsCreate);
        Assert.Equal("Products.Read", Permissions.ProductsRead);
        Assert.Equal("Products.Update", Permissions.ProductsUpdate);
        Assert.Equal("Products.Delete", Permissions.ProductsDelete);
        Assert.Equal("Products.ManageCategories", Permissions.ProductsManageCategories);

        Assert.Equal("Users.Create", Permissions.UsersCreate);
        Assert.Equal("Users.Read", Permissions.UsersRead);
        Assert.Equal("Users.Update", Permissions.UsersUpdate);
        Assert.Equal("Users.Delete", Permissions.UsersDelete);
        Assert.Equal("Users.ManageRoles", Permissions.UsersManageRoles);

        Assert.Equal("System.ViewLogs", Permissions.SystemViewLogs);
        Assert.Equal("System.ManageSettings", Permissions.SystemManageSettings);
        Assert.Equal("System.ManagePermissions", Permissions.SystemManagePermissions);
    }

    [Theory]
    [InlineData("Products")]
    [InlineData("PriceMonitoring")]
    [InlineData("Scrapers")]
    [InlineData("Users")]
    [InlineData("System")]
    public void PermissionCategories_ShouldExist(string expectedCategory)
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();

        // Act & Assert
        Assert.True(categories.ContainsKey(expectedCategory));
        Assert.NotEmpty(categories[expectedCategory]);
    }
}
