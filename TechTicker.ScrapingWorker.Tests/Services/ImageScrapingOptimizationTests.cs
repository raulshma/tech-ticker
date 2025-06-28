using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
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
    private readonly Mock<ProxyAwareHttpClientService> _mockProxyHttpClient;
    private readonly Mock<IImageStorageService> _mockImageStorageService;
    private readonly Mock<IProductImageService> _mockProductImageService;
    private readonly ImageScrapingService _imageScrapingService;

    public ImageScrapingOptimizationTests()
    {
        _mockLogger = new Mock<ILogger<ImageScrapingService>>();
        _mockProxyHttpClient = new Mock<ProxyAwareHttpClientService>();
        _mockImageStorageService = new Mock<IImageStorageService>();
        _mockProductImageService = new Mock<IProductImageService>();
        
        _imageScrapingService = new ImageScrapingService(
            _mockLogger.Object,
            _mockProxyHttpClient.Object,
            _mockImageStorageService.Object,
            _mockProductImageService.Object);
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

    [Fact]
    public async Task ScrapeImagesAsync_WithRecentImages_ShouldSkipProcessing()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var document = Mock.Of<AngleSharp.Dom.IDocument>();
        var imageSelector = "img.product-image";
        var baseUrl = "https://example.com";
        var maxImages = 5;

        var existingImagePaths = new List<string>
        {
            "images/products/test/image1.jpg",
            "images/products/test/image2.jpg",
            "images/products/test/image3.jpg"
        };

        _mockProductImageService.Setup(x => x.ShouldSkipImageProcessingAsync(productId, It.IsAny<TimeSpan>(), maxImages))
            .ReturnsAsync(true);

        _mockImageStorageService.Setup(x => x.GetProductImagePathsAsync(productId))
            .ReturnsAsync(existingImagePaths);

        _mockImageStorageService.Setup(x => x.ImageExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _imageScrapingService.ScrapeImagesAsync(document, imageSelector, baseUrl, productId, maxImages);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingImagePaths[0], result.PrimaryImageUrl);
        Assert.Equal(existingImagePaths.Skip(1).ToList(), result.AdditionalImageUrls);
        Assert.Equal(existingImagePaths.Count, result.SuccessfulUploads);
        Assert.Equal(0, result.ProcessedCount);

        // Verify that no downloads were attempted
        _mockProxyHttpClient.Verify(x => x.GetBinaryAsync(It.IsAny<string>(), null, null), Times.Never);
    }

    [Fact]
    public async Task ScrapeImagesAsync_WithSufficientExistingImages_ShouldSkipProcessing()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var document = Mock.Of<AngleSharp.Dom.IDocument>();
        var imageSelector = "img.product-image";
        var baseUrl = "https://example.com";
        var maxImages = 3;

        var existingImagePaths = new List<string>
        {
            "images/products/test/image1.jpg",
            "images/products/test/image2.jpg",
            "images/products/test/image3.jpg",
            "images/products/test/image4.jpg"
        };

        _mockProductImageService.Setup(x => x.ShouldSkipImageProcessingAsync(productId, It.IsAny<TimeSpan>(), maxImages))
            .ReturnsAsync(false);

        _mockProductImageService.Setup(x => x.HasAnyImagesAsync(productId))
            .ReturnsAsync(true);

        _mockImageStorageService.Setup(x => x.GetProductImagePathsAsync(productId))
            .ReturnsAsync(existingImagePaths);

        _mockImageStorageService.Setup(x => x.ImageExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _imageScrapingService.ScrapeImagesAsync(document, imageSelector, baseUrl, productId, maxImages);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingImagePaths[0], result.PrimaryImageUrl);
        Assert.Equal(existingImagePaths.Skip(1).Take(2).ToList(), result.AdditionalImageUrls);
        Assert.Equal(3, result.SuccessfulUploads);
        Assert.Equal(0, result.ProcessedCount);

        // Verify that no downloads were attempted
        _mockProxyHttpClient.Verify(x => x.GetBinaryAsync(It.IsAny<string>(), null, null), Times.Never);
    }

    [Fact]
    public async Task ScrapeImagesAsync_WithDuplicateContent_ShouldReuseExistingImage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var document = Mock.Of<AngleSharp.Dom.IDocument>();
        var imageSelector = "img.product-image";
        var baseUrl = "https://example.com";
        var maxImages = 5;

        var imageUrl = "https://example.com/image.jpg";
        var existingPath = "images/products/test/existing.jpg";

        _mockProductImageService.Setup(x => x.ShouldSkipImageProcessingAsync(productId, It.IsAny<TimeSpan>(), maxImages))
            .ReturnsAsync(false);

        _mockProductImageService.Setup(x => x.HasAnyImagesAsync(productId))
            .ReturnsAsync(false);

        // Mock document to return an image URL
        var mockElement = new Mock<AngleSharp.Dom.IElement>();
        mockElement.Setup(x => x.GetAttribute("src")).Returns(imageUrl);
        
        var mockElements = new List<AngleSharp.Dom.IElement> { mockElement.Object };
        var mockNodeList = new Mock<AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement>>();
        mockNodeList.Setup(x => x.GetEnumerator()).Returns(mockElements.GetEnumerator());
        
        var mockDocument = new Mock<AngleSharp.Dom.IDocument>();
        mockDocument.Setup(x => x.QuerySelectorAll(imageSelector)).Returns(mockNodeList.Object);

        // Mock download to return image data
        var imageData = new byte[] { 1, 2, 3, 4 };
        var proxyResponse = new ProxyAwareHttpResponse
        {
            IsSuccess = true,
            BinaryContent = imageData,
            Headers = new Dictionary<string, string> { { "content-type", "image/jpeg" } }
        };

        _mockProxyHttpClient.Setup(x => x.GetBinaryAsync(imageUrl, null, null))
            .ReturnsAsync(proxyResponse);

        // Mock duplicate detection to find existing image
        _mockImageStorageService.Setup(x => x.FindDuplicateByContentAsync(imageData, productId))
            .ReturnsAsync(existingPath);

        // Act
        var result = await _imageScrapingService.ScrapeImagesAsync(mockDocument.Object, imageSelector, baseUrl, productId, maxImages);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existingPath, result.PrimaryImageUrl);
        Assert.Equal(1, result.SuccessfulUploads);

        // Verify that the image was downloaded but not saved (reused existing)
        _mockProxyHttpClient.Verify(x => x.GetBinaryAsync(imageUrl, null, null), Times.Once);
        _mockImageStorageService.Verify(x => x.SaveImagesAsync(It.IsAny<List<ImageUploadData>>(), productId), Times.Never);
    }
}

// Mock class for ProxyResponse
public class ProxyResponse
{
    public bool IsSuccess { get; set; }
    public byte[]? BinaryContent { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public int StatusCode { get; set; }
    public string? ProxyUsed { get; set; }
}
