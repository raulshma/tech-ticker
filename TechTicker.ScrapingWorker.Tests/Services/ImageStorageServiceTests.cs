using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class ImageStorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<ImageStorageService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly string _testImagePath;
    private readonly ImageStorageService _imageStorageService;

    public ImageStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<ImageStorageService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Use a temporary directory for testing
        _testImagePath = Path.Combine(Path.GetTempPath(), "TechTicker_Test_Images", Guid.NewGuid().ToString());
        
        // Setup configuration mock to return our test path
        _mockConfiguration.Setup(x => x["ImageStorage:BasePath"])
            .Returns(_testImagePath);
        
        _imageStorageService = new ImageStorageService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task SaveImageAsync_WithValidImageData_ShouldSaveToConfiguredPath()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        
        // Create a simple test image (1x1 pixel JPEG)
        var imageData = new byte[] 
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01,
            0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0xFF, 0xC4,
            0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x0C,
            0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0xB2, 0xC0,
            0x07, 0xFF, 0xD9
        };

        // Act
        var result = await _imageStorageService.SaveImageAsync(imageData, fileName, contentType, productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith("images/products/");
        result.Should().Contain(productId.ToString());
        result.Should().EndWith(".jpg");

        // Verify the file was actually saved to the configured path
        var expectedDirectory = Path.Combine(_testImagePath, productId.ToString());
        Directory.Exists(expectedDirectory).Should().BeTrue();
        
        var files = Directory.GetFiles(expectedDirectory, "*.jpg");
        files.Should().HaveCount(1);
        
        var savedFile = files.First();
        var savedImageData = await File.ReadAllBytesAsync(savedFile);
        savedImageData.Should().Equal(imageData);
    }

    [Fact]
    public async Task SaveImageAsync_WithEmptyImageData_ShouldReturnNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fileName = "empty-image.jpg";
        var contentType = "image/jpeg";
        var emptyImageData = Array.Empty<byte>();

        // Act
        var result = await _imageStorageService.SaveImageAsync(emptyImageData, fileName, contentType, productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveImageAsync_WithUnsupportedContentType_ShouldReturnNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fileName = "test-file.txt";
        var contentType = "text/plain";
        var imageData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await _imageStorageService.SaveImageAsync(imageData, fileName, contentType, productId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public async Task SaveImageAsync_WithSupportedContentTypes_ShouldSaveSuccessfully(string contentType)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fileName = $"test-image.{contentType.Split('/')[1]}";
        var imageData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }; // Minimal test data

        // Act
        var result = await _imageStorageService.SaveImageAsync(imageData, fileName, contentType, productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith("images/products/");
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testImagePath))
        {
            Directory.Delete(_testImagePath, recursive: true);
        }
    }
}
