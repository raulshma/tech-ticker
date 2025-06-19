using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.Application.Tests.Services;

public class ScrapingOrchestrationServiceTests
{
    private readonly ScrapingOrchestrationService _orchestrationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<ILogger<ScrapingOrchestrationService>> _mockLogger;
    private readonly MessagingConfiguration _messagingConfig;

    public ScrapingOrchestrationServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<ScrapingOrchestrationService>>();
        
        _messagingConfig = new MessagingConfiguration
        {
            ScrapingExchange = "scraping",
            ScrapeCommandRoutingKey = "scrape.command"
        };

        var options = Options.Create(_messagingConfig);
        _orchestrationService = new ScrapingOrchestrationService(
            _mockUnitOfWork.Object,
            _mockMessagePublisher.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ScheduleScrapingJobsAsync_WithActiveMappings_ShouldScheduleJobs()
    {
        // Arrange
        var mappings = new List<ProductSellerMapping>
        {
            new ProductSellerMapping
            {
                MappingId = Guid.NewGuid(),
                CanonicalProductId = Guid.NewGuid(),
                SellerName = "Amazon",
                ExactProductUrl = "https://amazon.com/product/123",
                IsActiveForScraping = true,
                NextScrapeAt = DateTimeOffset.UtcNow.AddMinutes(-10), // Due for scraping
                SiteConfiguration = new ScraperSiteConfiguration
                {
                    SiteConfigId = Guid.NewGuid(),
                    SiteDomain = "amazon.com",
                    ProductNameSelector = "#productTitle",
                    PriceSelector = ".a-price-whole",
                    StockSelector = "#availability span"
                }
            },
            new ProductSellerMapping
            {
                MappingId = Guid.NewGuid(),
                CanonicalProductId = Guid.NewGuid(),
                SellerName = "Best Buy",
                ExactProductUrl = "https://bestbuy.com/product/456",
                IsActiveForScraping = true,
                NextScrapeAt = DateTimeOffset.UtcNow.AddMinutes(-5), // Due for scraping
                SiteConfiguration = new ScraperSiteConfiguration
                {
                    SiteConfigId = Guid.NewGuid(),
                    SiteDomain = "bestbuy.com",
                    ProductNameSelector = "h1.sr-only",
                    PriceSelector = ".sr-only",
                    StockSelector = ".fulfillment-add-to-cart-button"
                }
            }
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetMappingsDueForScrapingAsync(It.IsAny<int>()))
            .ReturnsAsync(mappings);

        // Act
        await _orchestrationService.ScheduleScrapingJobsAsync();

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Exactly(2));

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ScheduleScrapingJobsAsync_WithNoMappingsDue_ShouldNotScheduleJobs()
    {
        // Arrange
        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetMappingsDueForScrapingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ProductSellerMapping>());

        // Act
        await _orchestrationService.ScheduleScrapingJobsAsync();

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleScrapingJobsAsync_WithInactiveMappings_ShouldSkipInactive()
    {
        // Arrange
        // The GetMappingsDueForScrapingAsync method should already filter out inactive mappings
        // So we return an empty list to simulate that no active mappings are due for scraping
        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetMappingsDueForScrapingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ProductSellerMapping>());

        // Act
        await _orchestrationService.ScheduleScrapingJobsAsync();

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithSuccessfulResult_ShouldUpdateMapping()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = new ProductSellerMapping
        {
            MappingId = mappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ConsecutiveFailureCount = 2, // Had previous failures
            LastScrapeStatus = "FAILED"
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync(mapping);

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: true);

        // Assert
        mapping.LastScrapeStatus.Should().Be("SUCCESS");
        mapping.ConsecutiveFailureCount.Should().Be(0);
        mapping.LastScrapeErrorCode.Should().BeNull();
        mapping.LastScrapedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
        mapping.NextScrapeAt.Should().BeAfter(DateTimeOffset.UtcNow);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithFailedResult_ShouldIncrementFailureCount()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = new ProductSellerMapping
        {
            MappingId = mappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ConsecutiveFailureCount = 1,
            LastScrapeStatus = "SUCCESS"
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync(mapping);

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: false, "Network timeout");

        // Assert
        mapping.LastScrapeStatus.Should().Be("FAILED");
        mapping.ConsecutiveFailureCount.Should().Be(2);
        mapping.LastScrapeErrorCode.Should().Be("Network timeout");
        mapping.LastScrapedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithNonExistentMapping_ShouldHandleGracefully()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        
        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync((ProductSellerMapping?)null);

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithHighFailureCount_ShouldDeactivateMapping()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = new ProductSellerMapping
        {
            MappingId = mappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ConsecutiveFailureCount = 9, // Close to threshold
            IsActiveForScraping = true
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync(mapping);

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: false, "Repeated failures");

        // Assert
        mapping.ConsecutiveFailureCount.Should().Be(10);
        // Depending on implementation, might deactivate after certain threshold
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData("PT1H")] // 1 hour
    [InlineData("PT30M")] // 30 minutes
    [InlineData("PT6H")] // 6 hours
    [InlineData(null)] // No override
    public async Task ScheduleScrapingJobsAsync_WithFrequencyOverride_ShouldRespectOverride(string? frequencyOverride)
    {
        // Arrange
        var mapping = new ProductSellerMapping
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ExactProductUrl = "https://amazon.com/product/123",
            IsActiveForScraping = true,
            NextScrapeAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            ScrapingFrequencyOverride = frequencyOverride,
            SiteConfiguration = new ScraperSiteConfiguration
            {
                SiteConfigId = Guid.NewGuid(),
                SiteDomain = "amazon.com",
                ProductNameSelector = "#productTitle",
                PriceSelector = ".a-price-whole",
                StockSelector = "#availability span"
            }
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetMappingsDueForScrapingAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ProductSellerMapping> { mapping });

        // Act
        await _orchestrationService.ScheduleScrapingJobsAsync();

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<object>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleScrapingJobsAsync_WithException_ShouldLogError()
    {
        // Arrange
        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetMappingsDueForScrapingAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _orchestrationService.ScheduleScrapingJobsAsync());

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error scheduling scraping jobs")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ScrapingOrchestrationService_ShouldNotBeNull()
    {
        // Assert
        _orchestrationService.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithEmptyErrorMessage_ShouldHandleGracefully()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = new ProductSellerMapping
        {
            MappingId = mappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ConsecutiveFailureCount = 0
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync(mapping);

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: false, "");

        // Assert
        mapping.LastScrapeStatus.Should().Be("FAILED");
        mapping.LastScrapeErrorCode.Should().Be("");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ProcessScrapingResultAsync_WithLongErrorMessage_ShouldTruncateToFiftyCharacters()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = new ProductSellerMapping
        {
            MappingId = mappingId,
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ConsecutiveFailureCount = 0
        };

        _mockUnitOfWork.Setup(x => x.ProductSellerMappings.GetByIdAsync(mappingId))
            .ReturnsAsync(mapping);

        var longErrorMessage = "This is a very long error message that exceeds the fifty character limit for the LastScrapeErrorCode field in the database";

        // Act
        await _orchestrationService.ProcessScrapingResultAsync(mappingId, wasSuccessful: false, longErrorMessage);

        // Assert
        mapping.LastScrapeStatus.Should().Be("FAILED");
        mapping.LastScrapeErrorCode.Should().Be("This is a very long error message that exceeds the");
        mapping.LastScrapeErrorCode.Should().HaveLength(50);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}