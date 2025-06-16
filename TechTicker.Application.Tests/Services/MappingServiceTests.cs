using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Domain.Entities;

namespace TechTicker.Application.Tests.Services;

public class MappingServiceTests
{
    private readonly MappingService _mappingService;

    public MappingServiceTests()
    {
        _mappingService = new MappingService();
    }

    #region Category Mapping Tests

    [Fact]
    public void MapToDto_Category_ShouldMapCorrectly()
    {
        // Arrange
        var category = new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic devices and accessories",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var dto = _mappingService.MapToDto(category);

        // Assert
        dto.CategoryId.Should().Be(category.CategoryId);
        dto.Name.Should().Be(category.Name);
        dto.Slug.Should().Be(category.Slug);
        dto.Description.Should().Be(category.Description);
        dto.CreatedAt.Should().Be(category.CreatedAt);
        dto.UpdatedAt.Should().Be(category.UpdatedAt);
    }

    [Fact]
    public void MapToEntity_CreateCategoryDto_ShouldMapCorrectly()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Books",
            Slug = "books",
            Description = "Books and literature"
        };

        // Act
        var entity = _mappingService.MapToEntity(createDto);

        // Assert
        entity.Name.Should().Be(createDto.Name);
        entity.Slug.Should().Be(createDto.Slug);
        entity.Description.Should().Be(createDto.Description);
    }

    [Fact]
    public void MapToEntity_UpdateCategoryDto_ShouldUpdateExistingEntity()
    {
        // Arrange
        var existingCategory = new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = "Old Name",
            Slug = "old-slug",
            Description = "Old description",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var updateDto = new UpdateCategoryDto
        {
            Name = "New Name",
            Slug = "new-slug",
            Description = "New description"
        };

        // Act
        _mappingService.MapToEntity(updateDto, existingCategory);

        // Assert
        existingCategory.Name.Should().Be(updateDto.Name);
        existingCategory.Slug.Should().Be(updateDto.Slug);
        existingCategory.Description.Should().Be(updateDto.Description);
    }

    #endregion

    #region Product Mapping Tests

    [Fact]
    public void MapToDto_Product_ShouldMapCorrectly()
    {
        // Arrange
        var category = new Category { CategoryId = Guid.NewGuid(), Name = "Electronics" };
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = "iPhone 15",
            Manufacturer = "Apple",
            ModelNumber = "A3108",
            SKU = "APL-IP15-256",
            CategoryId = category.CategoryId,
            Category = category,
            Description = "Latest iPhone",
            Specifications = """{"storage": "256GB", "color": "Black"}""",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var dto = _mappingService.MapToDto(product);

        // Assert
        dto.ProductId.Should().Be(product.ProductId);
        dto.Name.Should().Be(product.Name);
        dto.Manufacturer.Should().Be(product.Manufacturer);
        dto.ModelNumber.Should().Be(product.ModelNumber);
        dto.SKU.Should().Be(product.SKU);
        dto.CategoryId.Should().Be(product.CategoryId);
        dto.Description.Should().Be(product.Description);
        dto.IsActive.Should().Be(product.IsActive);
        // Test navigation property mapping
        dto.Category.Should().NotBeNull();
        dto.Category!.Name.Should().Be(category.Name);
    }

    [Fact]
    public void MapToEntity_CreateProductDto_ShouldMapCorrectly()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "MacBook Pro",
            Manufacturer = "Apple",
            ModelNumber = "M3",
            SKU = "APL-MBP-M3",
            CategoryId = Guid.NewGuid(),
            Description = "Professional laptop",
            Specifications = new Dictionary<string, object>
            {
                { "cpu", "M3" },
                { "ram", "16GB" }
            }
        };

        // Act
        var entity = _mappingService.MapToEntity(createDto);

        // Assert
        entity.Name.Should().Be(createDto.Name);
        entity.Manufacturer.Should().Be(createDto.Manufacturer);
        entity.ModelNumber.Should().Be(createDto.ModelNumber);
        entity.SKU.Should().Be(createDto.SKU);
        entity.CategoryId.Should().Be(createDto.CategoryId);
        entity.Description.Should().Be(createDto.Description);
        entity.IsActive.Should().BeTrue();
    }

    #endregion

    #region User Mapping Tests

    [Fact]
    public void MapToDto_ApplicationUser_ShouldMapCorrectly()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-1),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var roles = new[] { "User", "Premium" };

        // Act
        var dto = _mappingService.MapToDto(user, roles);

        // Assert
        dto.UserId.Should().Be(user.Id);
        dto.Email.Should().Be(user.Email);
        dto.FirstName.Should().Be(user.FirstName);
        dto.LastName.Should().Be(user.LastName);
        dto.FullName.Should().Be("John Doe");
        dto.IsActive.Should().Be(user.IsActive);
        dto.Roles.Should().BeEquivalentTo(roles);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public void MapToEntity_CreateUserDto_ShouldMapCorrectly()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Email = "jane.smith@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Password = "SecurePassword123!"
        };

        // Act
        var entity = _mappingService.MapToEntity(createDto);

        // Assert
        entity.Email.Should().Be(createDto.Email);
        entity.FirstName.Should().Be(createDto.FirstName);
        entity.LastName.Should().Be(createDto.LastName);
        entity.IsActive.Should().BeTrue();
    }

    #endregion

    #region Edge Cases and Basic Functionality

    [Fact]
    public void MapToDto_ProductWithNullCategory_ShouldHandleGracefully()
    {
        // Arrange
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            Category = null!
        };

        // Act
        var dto = _mappingService.MapToDto(product);

        // Assert
        dto.ProductId.Should().Be(product.ProductId);
        dto.Name.Should().Be(product.Name);
        dto.CategoryId.Should().Be(product.CategoryId);
        dto.Category.Should().BeNull();
    }

    [Fact]
    public void MapToDto_UserWithNullNames_ShouldHandleGracefully()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = null,
            LastName = null
        };

        // Act
        var dto = _mappingService.MapToDto(user, Array.Empty<string>());

        // Assert
        dto.FirstName.Should().BeNull();
        dto.LastName.Should().BeNull();
        dto.FullName.Should().Be("");
    }

    [Fact]
    public void MappingService_ShouldNotBeNull()
    {
        // Assert
        _mappingService.Should().NotBeNull();
    }

    #endregion
}