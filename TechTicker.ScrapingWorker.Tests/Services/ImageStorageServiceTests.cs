using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
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

    [Fact]
    public async Task ImageExistsAsync_WithValidImage_ShouldReturnTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var relativePath = Path.Combine("images", "products", productId.ToString(), "test-image.jpg");
        var fullPath = Path.Combine(_testImagePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        // Create directory structure
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Create a valid JPEG file (minimal JPEG header)
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        await File.WriteAllBytesAsync(fullPath, jpegHeader);

        // Act
        var result = await _imageStorageService.ImageExistsAsync(relativePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ImageExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var relativePath = "images/products/test/non-existent-image.jpg";

        // Act
        var result = await _imageStorageService.ImageExistsAsync(relativePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImageExistsAsync_WithEmptyFile_ShouldReturnFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var relativePath = Path.Combine("images", "products", productId.ToString(), "empty-image.jpg");
        var fullPath = Path.Combine(_testImagePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        // Create directory structure
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Create an empty file
        await File.WriteAllBytesAsync(fullPath, Array.Empty<byte>());

        // Act
        var result = await _imageStorageService.ImageExistsAsync(relativePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImageExistsAsync_WithNullOrEmptyPath_ShouldReturnFalse()
    {
        // Act & Assert
        var result1 = await _imageStorageService.ImageExistsAsync(null!);
        result1.Should().BeFalse();

        var result2 = await _imageStorageService.ImageExistsAsync("");
        result2.Should().BeFalse();

        var result3 = await _imageStorageService.ImageExistsAsync("   ");
        result3.Should().BeFalse();
    }

    [Fact]
    public async Task FindDuplicateByContentAsync_WithDuplicateImage_ShouldReturnExistingPath()
    {
        // Arrange
        var productId = Guid.NewGuid();
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

        // Save the first image
        var firstPath = await _imageStorageService.SaveImageAsync(imageData, "test1.jpg", "image/jpeg", productId);
        firstPath.Should().NotBeNull();

        // Act - try to save the same image data again
        var duplicatePath = await _imageStorageService.FindDuplicateByContentAsync(imageData, productId);

        // Assert
        duplicatePath.Should().NotBeNull();
        duplicatePath.Should().Be(firstPath);
    }

    [Fact]
    public async Task FindDuplicateByContentAsync_WithDifferentImage_ShouldReturnNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var imageData1 = new byte[] 
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

        // Make imageData2 different from imageData1
        var imageData2 = (byte[])imageData1.Clone();
        imageData2[imageData2.Length - 1] = 0xAA; // Change last byte

        // Save the first image
        await _imageStorageService.SaveImageAsync(imageData1, "test1.jpg", "image/jpeg", productId);

        // Act - try to find duplicate with different image data
        var duplicatePath = await _imageStorageService.FindDuplicateByContentAsync(imageData2, productId);

        // Assert
        duplicatePath.Should().BeNull();
    }

    [Fact]
    public async Task FindDuplicateByContentAsync_WithEmptyImageData_ShouldReturnNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var emptyImageData = Array.Empty<byte>();

        // Act
        var duplicatePath = await _imageStorageService.FindDuplicateByContentAsync(emptyImageData, productId);

        // Assert
        duplicatePath.Should().BeNull();
    }

    [Fact]
    public async Task GetProductImagePathsAsync_WithNoImages_ShouldReturnEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var paths = await _imageStorageService.GetProductImagePathsAsync(productId);

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductImagePathsAsync_WithImages_ShouldReturnAllPaths()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var imageData1 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var imageData2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x47 }; // Different last byte

        // Save multiple different images
        var path1 = await _imageStorageService.SaveImageAsync(imageData1, "test1.jpg", "image/jpeg", productId);
        var path2 = await _imageStorageService.SaveImageAsync(imageData2, "test2.png", "image/png", productId);

        // Act
        var paths = await _imageStorageService.GetProductImagePathsAsync(productId);

        // Assert
        paths.Should().HaveCount(2);
        paths.Should().Contain(path1);
        paths.Should().Contain(path2);
    }

    [Fact]
    public void GenerateContentHash_WithSameData_ShouldReturnSameHash()
    {
        // Arrange
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };

        // Act
        var hash1 = _imageStorageService.GenerateContentHash(imageData);
        var hash2 = _imageStorageService.GenerateContentHash(imageData);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateContentHash_WithDifferentData_ShouldReturnDifferentHashes()
    {
        // Arrange
        var imageData1 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var imageData2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x47 }; // Different last byte

        // Act
        var hash1 = _imageStorageService.GenerateContentHash(imageData1);
        var hash2 = _imageStorageService.GenerateContentHash(imageData2);

        // Assert
        hash1.Should().NotBe(hash2);
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
