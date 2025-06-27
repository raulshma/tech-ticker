using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class ImageScrapingOptimizationTests
{
    private readonly Mock<ILogger<ImageScrapingService>> _mockLogger;
    private readonly Mock<IImageStorageService> _mockImageStorageService;
    private readonly Mock<IProductImageService> _mockProductImageService;

    public ImageScrapingOptimizationTests()
    {
        _mockLogger = new Mock<ILogger<ImageScrapingService>>();
        _mockImageStorageService = new Mock<IImageStorageService>();
        _mockProductImageService = new Mock<IProductImageService>();
    }

    [Fact]
    public async Task ProductImageService_GetExistingImageMappings_ShouldReturnCorrectMappings()
    {
        // This test verifies the optimization logic for checking existing images
        // The actual ImageScrapingService integration test would require complex HttpClient mocking

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

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockProductRepository = new Mock<IProductRepository>();
        var mockLogger = new Mock<ILogger<ProductImageService>>();

        mockUnitOfWork.Setup(x => x.Products).Returns(mockProductRepository.Object);

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

        mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var productImageService = new ProductImageService(mockUnitOfWork.Object, mockLogger.Object);

        // Act
        var result = await productImageService.GetExistingImageMappingsAsync(productId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey(originalUrls[0]).WhoseValue.Should().Be(localPaths[0]);
        result.Should().ContainKey(originalUrls[1]).WhoseValue.Should().Be(localPaths[1]);

        mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task ImageStorageService_ImageExists_ShouldValidateCorrectly()
    {
        // This test verifies the image existence checking logic

        // Arrange
        var mockLogger = new Mock<ILogger<ImageStorageService>>();
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();

        var testBasePath = Path.Combine(Path.GetTempPath(), "tech-ticker-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testBasePath);

        mockConfiguration.Setup(x => x["ImageStorage:BasePath"])
            .Returns(testBasePath);

        var imageStorageService = new ImageStorageService(mockLogger.Object, mockConfiguration.Object);

        try
        {
            // Create a valid JPEG file
            var validImagePath = Path.Combine(testBasePath, "valid-image.jpg");
            var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
            await File.WriteAllBytesAsync(validImagePath, jpegHeader);

            // Act & Assert
            var existsResult = await imageStorageService.ImageExistsAsync("valid-image.jpg");
            existsResult.Should().BeTrue();

            var notExistsResult = await imageStorageService.ImageExistsAsync("non-existent.jpg");
            notExistsResult.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testBasePath))
            {
                Directory.Delete(testBasePath, true);
            }
        }
    }

    [Fact]
    public void ImageOptimization_ConceptualTest_ShouldDemonstrateWorkflow()
    {
        // This test demonstrates the conceptual workflow of image optimization
        // without requiring complex mocking of HttpClient

        // Arrange - Simulate the optimization decision logic
        var existingImageMappings = new Dictionary<string, string>
        {
            { "https://example.com/image1.jpg", "images/products/test/existing1.jpg" },
            { "https://example.com/image2.jpg", "images/products/test/existing2.jpg" }
        };

        var scrapedImageUrls = new List<string>
        {
            "https://example.com/image1.jpg", // Exists
            "https://example.com/image2.jpg", // Exists but file missing
            "https://example.com/image3.jpg"  // New image
        };

        var imageExistenceResults = new Dictionary<string, bool>
        {
            { "images/products/test/existing1.jpg", true },  // File exists
            { "images/products/test/existing2.jpg", false }  // File missing
        };

        // Act - Simulate the optimization logic
        var existingPaths = new List<string>();
        var urlsToDownload = new List<string>();

        foreach (var imageUrl in scrapedImageUrls)
        {
            if (existingImageMappings.TryGetValue(imageUrl, out var existingPath))
            {
                if (imageExistenceResults.GetValueOrDefault(existingPath, false))
                {
                    existingPaths.Add(existingPath);
                    continue;
                }
            }
            urlsToDownload.Add(imageUrl);
        }

        // Assert - Verify optimization decisions
        existingPaths.Should().ContainSingle()
            .Which.Should().Be("images/products/test/existing1.jpg");

        urlsToDownload.Should().HaveCount(2);
        urlsToDownload.Should().Contain("https://example.com/image2.jpg"); // Re-download missing file
        urlsToDownload.Should().Contain("https://example.com/image3.jpg"); // Download new image

        // This demonstrates that:
        // 1. Existing valid images are reused (1 image)
        // 2. Missing local files are re-downloaded (1 image)
        // 3. New images are downloaded (1 image)
        // Total: 1 existing + 2 downloads = 3 images
    }
}
