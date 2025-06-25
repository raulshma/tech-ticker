using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class PriceDataProcessingServiceTests
{
    private readonly PriceDataProcessingService _priceDataProcessingService;
    private readonly Mock<ILogger<PriceDataProcessingService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly MessagingConfiguration _messagingConfig;

    public PriceDataProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<PriceDataProcessingService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        
        _messagingConfig = new MessagingConfiguration
        {
            PriceDataExchange = "price-data",
            PricePointRecordedRoutingKey = "price.recorded"
        };

        var options = Options.Create(_messagingConfig);
        _priceDataProcessingService = new PriceDataProcessingService(
            _mockUnitOfWork.Object,
            _mockMessagePublisher.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithValidData_ShouldProcessSuccessfully()
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            ScrapedProductName = "iPhone 15 Pro",
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockUnitOfWork.Setup(x => x.Products.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.Product, bool>>>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.PriceHistory.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.PriceHistory, bool>>>()))
            .ReturnsAsync(new List<TechTicker.Domain.Entities.PriceHistory>());

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<PricePointRecordedEvent>(),
            _messagingConfig.PriceDataExchange,
            _messagingConfig.PricePointRecordedRoutingKey), Times.Once);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithNullData_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _priceDataProcessingService.ProcessRawPriceDataAsync(null!));
    }

    [Theory]
    [InlineData("IN STOCK")]
    [InlineData("in stock")]
    [InlineData("Available")]
    [InlineData("AVAILABLE")]
    public async Task ProcessRawPriceDataAsync_WithVariousInStockStatuses_ShouldNormalize(string stockStatus)
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = stockStatus,
            SourceUrl = "https://amazon.com/product/123",
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockUnitOfWork.Setup(x => x.Products.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.Product, bool>>>()))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.PriceHistory.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.PriceHistory, bool>>>()))
            .ReturnsAsync(new List<TechTicker.Domain.Entities.PriceHistory>());

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithNegativePrice_ShouldNotProcess()
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ScrapedPrice = -100m, // Invalid negative price
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://example.com/product/test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Should not add to price history due to invalid price
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Never);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithZeroPrice_ShouldNotProcess()
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ScrapedPrice = 0m, // Invalid zero price
            ScrapedStockStatus = "Out of Stock",
            SourceUrl = "https://example.com/product/test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Should not add to price history due to invalid price
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Never);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithNonExistentProduct_ShouldNotProcess()
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockUnitOfWork.Setup(x => x.Products.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.Product, bool>>>()))
            .ReturnsAsync(false); // Product doesn't exist

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Should not add to price history due to non-existent product
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProcessRawPriceDataAsync_WithInvalidSellerName_ShouldNotProcess(string? sellerName)
    {
        // Arrange
        var rawData = new RawPriceDataEvent
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = sellerName!,
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://example.com/product/test",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Should not add to price history due to invalid seller name
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Never);
    }

    [Fact]
    public void PriceDataProcessingService_ShouldNotBeNull()
    {
        // Assert
        _priceDataProcessingService.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithValidMappingId_ShouldUseMappingIdInPriceHistory()
    {
        // Arrange
        var expectedMappingId = Guid.NewGuid();
        var rawData = new RawPriceDataEvent
        {
            MappingId = expectedMappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            ScrapedProductName = "iPhone 15 Pro",
            Timestamp = DateTimeOffset.UtcNow
        };

        _mockUnitOfWork.Setup(x => x.Products.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.Product, bool>>>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.PriceHistory.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TechTicker.Domain.Entities.PriceHistory, bool>>>()))
            .ReturnsAsync(new List<TechTicker.Domain.Entities.PriceHistory>());

        TechTicker.Domain.Entities.PriceHistory? capturedPriceHistory = null;
        _mockUnitOfWork.Setup(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()))
            .Callback<TechTicker.Domain.Entities.PriceHistory>(ph => capturedPriceHistory = ph);

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<TechTicker.Domain.Entities.PriceHistory>()), Times.Once);
        Assert.NotNull(capturedPriceHistory);
        Assert.Equal(expectedMappingId, capturedPriceHistory.MappingId);
    }
}
