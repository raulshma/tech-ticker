using System.Text.Json;
using TechTicker.Domain.Entities;
using TechTicker.Domain.Entities.Canonical;

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
    public void NormalizedSpecificationsDict_WithValidJson_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var product = new Product();
        var normalizedSpecs = new Dictionary<string, NormalizedSpecificationValue>
        {
            ["screen_size"] = new() { Value = "6.1", Unit = "inches", DataType = SpecificationType.Dimension, RawValue = "6.1 inches", CanonicalName = "screen_size", Confidence = 0.95 },
            ["storage"] = new() { Value = 256, Unit = "GB", DataType = SpecificationType.Memory, RawValue = "256GB", CanonicalName = "storage", Confidence = 0.98 },
            ["ram"] = new() { Value = 8, Unit = "GB", DataType = SpecificationType.Memory, RawValue = "8GB", CanonicalName = "ram", Confidence = 0.97 }
        };

        // Act
        product.NormalizedSpecificationsDict = normalizedSpecs;
        var retrievedSpecs = product.NormalizedSpecificationsDict;

        // Assert
        product.NormalizedSpecifications.Should().NotBeNullOrEmpty();
        retrievedSpecs.Should().NotBeNull();
        retrievedSpecs.Should().ContainKey("screen_size");
        retrievedSpecs.Should().ContainKey("storage");
        retrievedSpecs.Should().ContainKey("ram");
    }

    [Fact]
    public void NormalizedSpecificationsDict_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var product = new Product();

        // Act
        product.NormalizedSpecificationsDict = null;

        // Assert
        product.NormalizedSpecifications.Should().BeNull();
        product.NormalizedSpecificationsDict.Should().BeNull();
    }

    [Fact]
    public void UncategorizedSpecificationsDict_WithValidJson_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var product = new Product();
        var uncategorizedSpecs = new Dictionary<string, string>
        {
            ["CPU"] = "A17 Pro",
            ["GPU"] = "6-core",
            ["Battery"] = "3274 mAh"
        };

        // Act
        product.UncategorizedSpecificationsDict = uncategorizedSpecs;
        var retrieved = product.UncategorizedSpecificationsDict;

        // Assert
        product.UncategorizedSpecifications.Should().NotBeNullOrEmpty();
        retrieved.Should().NotBeNull();
        retrieved.Should().ContainKey("CPU");
        retrieved.Should().ContainKey("GPU");
        retrieved.Should().ContainKey("Battery");
        retrieved!["CPU"].Should().Be("A17 Pro");
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
    public void Product_ImageUrls_ShouldHandleJsonSerialization()
    {
        // Arrange
        var product = new Product();
        var additionalImages = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" };
        var originalImages = new List<string> { "original1.jpg", "original2.jpg" };

        // Act
        product.AdditionalImageUrlsList = additionalImages;
        product.OriginalImageUrlsList = originalImages;

        // Assert
        product.AdditionalImageUrls.Should().NotBeNullOrEmpty();
        product.OriginalImageUrls.Should().NotBeNullOrEmpty();
        product.AdditionalImageUrlsList.Should().HaveCount(3);
        product.OriginalImageUrlsList.Should().HaveCount(2);
        product.AdditionalImageUrlsList.Should().Contain("image1.jpg");
        product.OriginalImageUrlsList.Should().Contain("original1.jpg");
    }
}