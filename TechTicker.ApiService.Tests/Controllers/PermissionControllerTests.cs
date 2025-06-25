using TechTicker.Shared.Constants;
using Xunit;

namespace TechTicker.ApiService.Tests.Controllers;

public class PermissionControllerTests
{
    [Fact]
    public void GetPermissionCategories_ShouldReturnCategories()
    {
        // Act & Assert - Just verify the constants are properly defined
        var categories = Permissions.GetPermissionsByCategory();

        Assert.NotEmpty(categories);
        Assert.True(categories.ContainsKey("Products"));
        Assert.True(categories.ContainsKey("Users"));
        Assert.True(categories.ContainsKey("System"));

        // Verify specific permissions exist
        Assert.Contains(Permissions.ProductsCreate, categories["Products"]);
        Assert.Contains(Permissions.ProductsRead, categories["Products"]);
        Assert.Contains(Permissions.UsersManageRoles, categories["Users"]);
        Assert.Contains(Permissions.SystemManagePermissions, categories["System"]);
    }

    [Fact]
    public void PermissionConstants_ShouldBeWellFormed()
    {
        // Arrange & Act
        var allPermissions = Permissions.GetAllPermissions();
        
        // Assert
        Assert.NotEmpty(allPermissions);
        
        // Verify all permissions follow naming convention
        foreach (var permission in allPermissions)
        {
            Assert.Contains(".", permission); // Should have category.action format
            Assert.DoesNotContain(" ", permission); // Should not contain spaces
            Assert.Equal(permission, permission.Trim()); // Should not have leading/trailing whitespace
        }
        
        // Verify no duplicates
        Assert.Equal(allPermissions.Length, allPermissions.Distinct().Count());
    }

    [Fact]
    public void PermissionCategories_ShouldCoverAllPermissions()
    {
        // Arrange
        var allPermissions = Permissions.GetAllPermissions();
        var categorizedPermissions = Permissions.GetPermissionsByCategory()
            .SelectMany(kvp => kvp.Value)
            .ToArray();
        
        // Act & Assert
        Assert.Equal(allPermissions.Length, categorizedPermissions.Length);
        
        foreach (var permission in allPermissions)
        {
            Assert.Contains(permission, categorizedPermissions);
        }
    }

    [Theory]
    [InlineData("Products.Create")]
    [InlineData("Products.Read")]
    [InlineData("Products.Update")]
    [InlineData("Products.Delete")]
    [InlineData("Users.ManageRoles")]
    [InlineData("System.ManagePermissions")]
    public void SpecificPermissions_ShouldExist(string expectedPermission)
    {
        // Arrange
        var allPermissions = Permissions.GetAllPermissions();
        
        // Act & Assert
        Assert.Contains(expectedPermission, allPermissions);
    }

    [Fact]
    public void PermissionCategories_ShouldHaveExpectedStructure()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        
        // Act & Assert
        Assert.True(categories.Count >= 5); // Should have at least 5 categories
        
        // Verify expected categories exist
        var expectedCategories = new[] { "Products", "PriceMonitoring", "Scrapers", "Users", "System" };
        foreach (var expectedCategory in expectedCategories)
        {
            Assert.True(categories.ContainsKey(expectedCategory), 
                $"Category '{expectedCategory}' should exist");
            Assert.NotEmpty(categories[expectedCategory]);
        }
    }

    [Fact]
    public void ProductPermissions_ShouldIncludeBasicCRUD()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        var productPermissions = categories["Products"];
        
        // Act & Assert
        Assert.Contains("Products.Create", productPermissions);
        Assert.Contains("Products.Read", productPermissions);
        Assert.Contains("Products.Update", productPermissions);
        Assert.Contains("Products.Delete", productPermissions);
        Assert.Contains("Products.ManageCategories", productPermissions);
    }

    [Fact]
    public void UserPermissions_ShouldIncludeRoleManagement()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        var userPermissions = categories["Users"];
        
        // Act & Assert
        Assert.Contains("Users.Create", userPermissions);
        Assert.Contains("Users.Read", userPermissions);
        Assert.Contains("Users.Update", userPermissions);
        Assert.Contains("Users.Delete", userPermissions);
        Assert.Contains("Users.ManageRoles", userPermissions);
    }

    [Fact]
    public void SystemPermissions_ShouldIncludeAdminFunctions()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        var systemPermissions = categories["System"];
        
        // Act & Assert
        Assert.Contains("System.ViewLogs", systemPermissions);
        Assert.Contains("System.ManageSettings", systemPermissions);
        Assert.Contains("System.ManagePermissions", systemPermissions);
    }

    [Fact]
    public void ScraperPermissions_ShouldIncludeManagementFunctions()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        var scraperPermissions = categories["Scrapers"];
        
        // Act & Assert
        Assert.Contains("Scrapers.Configure", scraperPermissions);
        Assert.Contains("Scrapers.Run", scraperPermissions);
        Assert.Contains("Scrapers.ViewLogs", scraperPermissions);
        Assert.Contains("Scrapers.ManageSites", scraperPermissions);
    }

    [Fact]
    public void PriceMonitoringPermissions_ShouldIncludeAlertManagement()
    {
        // Arrange
        var categories = Permissions.GetPermissionsByCategory();
        var pricePermissions = categories["PriceMonitoring"];
        
        // Act & Assert
        Assert.Contains("PriceHistory.Read", pricePermissions);
        Assert.Contains("AlertRules.Create", pricePermissions);
        Assert.Contains("AlertRules.Read", pricePermissions);
        Assert.Contains("AlertRules.Update", pricePermissions);
        Assert.Contains("AlertRules.Delete", pricePermissions);
    }
}
