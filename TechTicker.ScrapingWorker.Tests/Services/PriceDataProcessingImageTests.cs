using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class PriceDataProcessingImageTests
{
    private readonly PriceDataProcessingService _priceDataProcessingService;
    private readonly Mock<ILogger<PriceDataProcessingService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IPriceHistoryRepository> _mockPriceHistoryRepository;
    private readonly MessagingConfiguration _messagingConfig;

    public PriceDataProcessingImageTests()
    {
        _mockLogger = new Mock<ILogger<PriceDataProcessingService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockPriceHistoryRepository = new Mock<IPriceHistoryRepository>();
        
        _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(x => x.PriceHistory).Returns(_mockPriceHistoryRepository.Object);
        
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
    public async Task ProcessRawPriceDataAsync_WithImageData_ShouldUpdateProductImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();
        
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var rawData = new RawPriceDataEvent
        {
            MappingId = mappingId,
            CanonicalProductId = productId,
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            ScrapedProductName = "iPhone 15 Pro",
            Timestamp = DateTimeOffset.UtcNow,
            // Image data
            PrimaryImageUrl = "images/products/test/primary.jpg",
            AdditionalImageUrls = new List<string> { "images/products/test/additional1.jpg" },
            OriginalImageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" }
        };

        _mockProductRepository.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _mockPriceHistoryRepository.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceHistory, bool>>>()))
            .ReturnsAsync(new List<PriceHistory>());

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Verify product was retrieved
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        
        // Verify product was updated
        _mockUnitOfWork.Verify(x => x.Products.Update(It.Is<Product>(p => 
            p.ProductId == productId &&
            p.PrimaryImageUrl == "images/products/test/primary.jpg" &&
            p.ImageLastUpdated != null)), Times.Once);
        
        // Verify price history was added
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<PriceHistory>()), Times.Once);
        
        // Verify changes were saved
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
        
        // Verify message was published
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<PricePointRecordedEvent>(),
            _messagingConfig.PriceDataExchange,
            _messagingConfig.PricePointRecordedRoutingKey), Times.Once);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithoutImageData_ShouldNotUpdateProductImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();
        
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var rawData = new RawPriceDataEvent
        {
            MappingId = mappingId,
            CanonicalProductId = productId,
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            ScrapedProductName = "iPhone 15 Pro",
            Timestamp = DateTimeOffset.UtcNow
            // No image data
        };

        _mockProductRepository.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockPriceHistoryRepository.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceHistory, bool>>>()))
            .ReturnsAsync(new List<PriceHistory>());

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Verify product was NOT retrieved for image update
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Never);
        
        // Verify product was NOT updated
        _mockUnitOfWork.Verify(x => x.Products.Update(It.IsAny<Product>()), Times.Never);
        
        // Verify price history was still added
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<PriceHistory>()), Times.Once);
        
        // Verify changes were saved
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessRawPriceDataAsync_WithNonExistentProduct_ShouldNotUpdateImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        var rawData = new RawPriceDataEvent
        {
            MappingId = mappingId,
            CanonicalProductId = productId,
            SellerName = "Amazon",
            ScrapedPrice = 299.99m,
            ScrapedStockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/123",
            ScrapedProductName = "iPhone 15 Pro",
            Timestamp = DateTimeOffset.UtcNow,
            // Image data
            PrimaryImageUrl = "images/products/test/primary.jpg"
        };

        _mockProductRepository.Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null); // Product not found
        _mockPriceHistoryRepository.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PriceHistory, bool>>>()))
            .ReturnsAsync(new List<PriceHistory>());

        // Act
        await _priceDataProcessingService.ProcessRawPriceDataAsync(rawData);

        // Assert
        // Verify product was retrieved for image update
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
        
        // Verify product was NOT updated (since it doesn't exist)
        _mockUnitOfWork.Verify(x => x.Products.Update(It.IsAny<Product>()), Times.Never);
        
        // Verify price history was still added
        _mockUnitOfWork.Verify(x => x.PriceHistory.AddAsync(It.IsAny<PriceHistory>()), Times.Once);
        
        // Verify changes were saved
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}
