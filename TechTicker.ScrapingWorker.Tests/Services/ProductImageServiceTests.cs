using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class ProductImageServiceTests
{
    private readonly ProductImageService _productImageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ILogger<ProductImageService>> _mockLogger;

    public ProductImageServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<ProductImageService>>();

        _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);

        _productImageService = new ProductImageService(
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateProductImagesAsync_WithValidData_ShouldUpdateProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var primaryImageUrl = "images/products/test/primary.jpg";
        var additionalImageUrls = new List<string> { "images/products/test/additional1.jpg", "images/products/test/additional2.jpg" };
        var originalImageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" };

        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        await _productImageService.UpdateProductImagesAsync(
            productId,
            primaryImageUrl,
            additionalImageUrls,
            originalImageUrls);

        // Assert
        product.PrimaryImageUrl.Should().Be(primaryImageUrl);
        product.AdditionalImageUrlsList.Should().BeEquivalentTo(additionalImageUrls);
        product.OriginalImageUrlsList.Should().BeEquivalentTo(originalImageUrls);
        product.ImageLastUpdated.Should().NotBeNull();
        product.ImageLastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(x => x.Products.Update(product), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateProductImagesAsync_WithNonExistentProduct_ShouldLogWarningAndReturn()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var primaryImageUrl = "images/products/test/primary.jpg";

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        await _productImageService.UpdateProductImagesAsync(
            productId,
            primaryImageUrl,
            null,
            null);

        // Assert
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(x => x.Products.Update(It.IsAny<Product>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task HasRecentImagesAsync_WithRecentImages_ShouldReturnTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var maxAge = TimeSpan.FromHours(1);

        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            ImageLastUpdated = DateTimeOffset.UtcNow.AddMinutes(-30), // 30 minutes ago
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productImageService.HasRecentImagesAsync(productId, maxAge);

        // Assert
        result.Should().BeTrue();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task HasRecentImagesAsync_WithOldImages_ShouldReturnFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var maxAge = TimeSpan.FromHours(1);

        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            ImageLastUpdated = DateTimeOffset.UtcNow.AddHours(-2), // 2 hours ago
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productImageService.HasRecentImagesAsync(productId, maxAge);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetExistingImageMappingsAsync_WithExistingImages_ShouldReturnMappings()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalUrls = new List<string>
        {
            "https://example.com/image1.jpg",
            "https://example.com/image2.jpg"
        };
        var localPaths = new List<string>
        {
            "images/products/test/primary.jpg",
            "images/products/test/additional1.jpg"
        };

        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            PrimaryImageUrl = localPaths[0],
            AdditionalImageUrls = System.Text.Json.JsonSerializer.Serialize(localPaths.Skip(1).ToList()),
            OriginalImageUrls = System.Text.Json.JsonSerializer.Serialize(originalUrls),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productImageService.GetExistingImageMappingsAsync(productId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey(originalUrls[0]).WhoseValue.Should().Be(localPaths[0]);
        result.Should().ContainKey(originalUrls[1]).WhoseValue.Should().Be(localPaths[1]);
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetExistingImageMappingsAsync_WithNonExistentProduct_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productImageService.GetExistingImageMappingsAsync(productId);

        // Assert
        result.Should().BeEmpty();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task HasAnyImagesAsync_WithProductHavingImages_ShouldReturnTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            PrimaryImageUrl = "images/products/test/primary.jpg",
            AdditionalImageUrls = System.Text.Json.JsonSerializer.Serialize(new List<string> { "images/products/test/additional1.jpg" }),
            OriginalImageUrls = System.Text.Json.JsonSerializer.Serialize(new List<string> { "https://example.com/image1.jpg" }),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productImageService.HasAnyImagesAsync(productId);

        // Assert
        result.Should().BeTrue();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task HasAnyImagesAsync_WithProductHavingNoImages_ShouldReturnFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productImageService.HasAnyImagesAsync(productId);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task HasAnyImagesAsync_WithNonExistentProduct_ShouldReturnFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productImageService.HasAnyImagesAsync(productId);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }
}
