using System.Text.Json;
using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Product_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.IsActive.Should().BeTrue();
        product.ProductSellerMappings.Should().NotBeNull().And.BeEmpty();
        product.AlertRules.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Product_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var product = new Product
        {
            Name = "iPhone 15 Pro",
            Manufacturer = "Apple",
            ModelNumber = "A3108",
            SKU = "APL-IP15PRO-256GB",
            CategoryId = categoryId,
            Description = "Latest iPhone with advanced camera system",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        product.Name.Should().Be("iPhone 15 Pro");
        product.Manufacturer.Should().Be("Apple");
        product.ModelNumber.Should().Be("A3108");
        product.SKU.Should().Be("APL-IP15PRO-256GB");
        product.CategoryId.Should().Be(categoryId);
        product.Description.Should().Be("Latest iPhone with advanced camera system");
        product.CreatedAt.Should().Be(createdAt);
        product.UpdatedAt.Should().Be(createdAt);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SpecificationsDict_WithValidJson_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var product = new Product();
        var specifications = new Dictionary<string, object>
        {
            ["Screen Size"] = "6.1 inches",
            ["Storage"] = "256GB",
            ["RAM"] = "8GB",
            ["Colors"] = new[] { "Black", "White", "Blue", "Gold" }
        };

        // Act
        product.SpecificationsDict = specifications;
        var retrievedSpecs = product.SpecificationsDict;

        // Assert
        product.Specifications.Should().NotBeNullOrEmpty();
        retrievedSpecs.Should().NotBeNull();
        retrievedSpecs.Should().ContainKey("Screen Size");
        retrievedSpecs.Should().ContainKey("Storage");
        retrievedSpecs.Should().ContainKey("RAM");
        retrievedSpecs.Should().ContainKey("Colors");
    }

    [Fact]
    public void SpecificationsDict_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var product = new Product();

        // Act
        product.SpecificationsDict = null;

        // Assert
        product.Specifications.Should().BeNull();
        product.SpecificationsDict.Should().BeNull();
    }

    [Fact]
    public void SpecificationsDict_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        var product = new Product { Specifications = "" };

        // Act
        var specs = product.SpecificationsDict;

        // Assert
        specs.Should().BeNull();
    }

    [Fact]
    public void SpecificationsDict_WithValidJsonString_ShouldDeserialize()
    {
        // Arrange
        var jsonString = """{"CPU": "A17 Pro", "GPU": "6-core", "Battery": "3274 mAh"}""";
        var product = new Product { Specifications = jsonString };

        // Act
        var specs = product.SpecificationsDict;

        // Assert
        specs.Should().NotBeNull();
        specs.Should().ContainKey("CPU");
        specs.Should().ContainKey("GPU");
        specs.Should().ContainKey("Battery");
        specs!["CPU"].ToString().Should().Be("A17 Pro");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Product_WithInvalidName_ShouldHandleGracefully(string? name)
    {
        // Arrange & Act
        var product = new Product { Name = name! };

        // Assert
        product.Name.Should().Be(name);
    }

    [Fact]
    public void Product_WithCategoryId_ShouldSetCategoryIdCorrectly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var product = new Product
        {
            CategoryId = categoryId
        };

        // Assert
        product.CategoryId.Should().Be(categoryId);
        product.ProductSellerMappings.Should().NotBeNull().And.BeEmpty();
        product.AlertRules.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Product_Collections_ShouldAllowAddingItems()
    {
        // Arrange
        var product = new Product();
        var mapping = new ProductSellerMapping { MappingId = Guid.NewGuid() };
        var alertRule = new AlertRule { AlertRuleId = Guid.NewGuid() };

        // Act
        product.ProductSellerMappings.Add(mapping);
        product.AlertRules.Add(alertRule);

        // Assert
        product.ProductSellerMappings.Should().HaveCount(1);
        product.AlertRules.Should().HaveCount(1);
        product.ProductSellerMappings.Should().Contain(mapping);
        product.AlertRules.Should().Contain(alertRule);
    }

    [Fact]
    public void Product_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Product_WithComplexSpecifications_ShouldHandleNestedObjects()
    {
        // Arrange
        var product = new Product();
        var complexSpecs = new Dictionary<string, object>
        {
            ["Dimensions"] = new Dictionary<string, object>
            {
                ["Height"] = "159.9mm",
                ["Width"] = "76.7mm",
                ["Depth"] = "8.25mm"
            },
            ["Weight"] = "221g",
            ["Features"] = new[] { "Face ID", "Wireless Charging", "Water Resistant" }
        };

        // Act
        product.SpecificationsDict = complexSpecs;
        var retrieved = product.SpecificationsDict;

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().ContainKey("Dimensions");
        retrieved.Should().ContainKey("Weight");
        retrieved.Should().ContainKey("Features");
    }
}