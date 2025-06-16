using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Category_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        category.Products.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Category_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var category = new Category
        {
            Name = "Smartphones",
            Slug = "smartphones",
            Description = "Mobile phones and accessories",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        category.Name.Should().Be("Smartphones");
        category.Slug.Should().Be("smartphones");
        category.Description.Should().Be("Mobile phones and accessories");
        category.CreatedAt.Should().Be(createdAt);
        category.UpdatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("Electronics", "electronics")]
    [InlineData("Home & Garden", "home-garden")]
    [InlineData("Books & Media", "books-media")]
    [InlineData("Sports & Outdoors", "sports-outdoors")]
    public void Category_WithVariousNames_ShouldAcceptValidNames(string name, string slug)
    {
        // Arrange & Act
        var category = new Category
        {
            Name = name,
            Slug = slug
        };

        // Assert
        category.Name.Should().Be(name);
        category.Slug.Should().Be(slug);
    }

    [Fact]
    public void Category_ProductsCollection_ShouldAllowAddingProducts()
    {
        // Arrange
        var category = new Category();
        var product1 = new Product { ProductId = Guid.NewGuid(), Name = "Product 1" };
        var product2 = new Product { ProductId = Guid.NewGuid(), Name = "Product 2" };

        // Act
        category.Products.Add(product1);
        category.Products.Add(product2);

        // Assert
        category.Products.Should().HaveCount(2);
        category.Products.Should().Contain(product1);
        category.Products.Should().Contain(product2);
    }

    [Fact]
    public void Category_WithNullDescription_ShouldBeValid()
    {
        // Arrange & Act
        var category = new Category
        {
            Name = "Test Category",
            Slug = "test-category",
            Description = null
        };

        // Assert
        category.Description.Should().BeNull();
        category.Name.Should().Be("Test Category");
        category.Slug.Should().Be("test-category");
    }

    [Fact]
    public void Category_WithEmptyDescription_ShouldBeValid()
    {
        // Arrange & Act
        var category = new Category
        {
            Name = "Test Category",
            Slug = "test-category",
            Description = ""
        };

        // Assert
        category.Description.Should().Be("");
        category.Name.Should().Be("Test Category");
        category.Slug.Should().Be("test-category");
    }

    [Theory]
    [InlineData("smartphones")]
    [InlineData("laptop-computers")]
    [InlineData("gaming-consoles")]
    [InlineData("smart-home")]
    public void Category_WithValidSlugs_ShouldAcceptSlugFormat(string slug)
    {
        // Arrange & Act
        var category = new Category
        {
            Name = "Test Category",
            Slug = slug
        };

        // Assert
        category.Slug.Should().Be(slug);
    }

    [Fact]
    public void Category_ProductsCollection_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        category.Products.Should().NotBeNull();
        category.Products.Should().BeOfType<List<Product>>();
        category.Products.Should().BeEmpty();
    }

    [Fact]
    public void Category_WithLongDescription_ShouldHandleLargeText()
    {
        // Arrange
        var longDescription = new string('A', 1000); // 1000 character description

        // Act
        var category = new Category
        {
            Name = "Test Category",
            Slug = "test-category",
            Description = longDescription
        };

        // Assert
        category.Description.Should().Be(longDescription);
        category.Description.Should().HaveLength(1000);
    }

    [Fact]
    public void Category_CreatedAndUpdatedAt_ShouldTrackTimestamps()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var category = new Category
        {
            Name = "Test Category",
            Slug = "test-category",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        category.CreatedAt.Should().Be(createdAt);
        category.UpdatedAt.Should().Be(updatedAt);
        category.UpdatedAt.Should().BeAfter(category.CreatedAt);
    }
}