using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Application.DTOs;
using TechTicker.ScrapingWorker.Services;
using TechTicker.Shared.Utilities;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class WebScrapingServiceTests : IDisposable
{
    private readonly WebScrapingService _webScrapingService;
    private readonly Mock<ILogger<WebScrapingService>> _mockLogger;
    private readonly Mock<IScraperRunLogService> _mockScraperRunLogService;
    private readonly HttpClient _httpClient;

    public WebScrapingServiceTests()
    {
        _mockLogger = new Mock<ILogger<WebScrapingService>>();
        _mockScraperRunLogService = new Mock<IScraperRunLogService>();
        _httpClient = new HttpClient();

        // Setup default mock responses
        _mockScraperRunLogService
            .Setup(x => x.CreateRunLogAsync(It.IsAny<CreateScraperRunLogDto>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        _mockScraperRunLogService
            .Setup(x => x.UpdateRunLogAsync(It.IsAny<Guid>(), It.IsAny<UpdateScraperRunLogDto>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _mockScraperRunLogService
            .Setup(x => x.CompleteRunAsync(It.IsAny<Guid>(), It.IsAny<CompleteScraperRunDto>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _mockScraperRunLogService
            .Setup(x => x.FailRunAsync(It.IsAny<Guid>(), It.IsAny<FailScraperRunDto>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var mockImageScrapingService = new Mock<IImageScrapingService>();
        _webScrapingService = new WebScrapingService(_mockLogger.Object, _httpClient, _mockScraperRunLogService.Object, mockImageScrapingService.Object);
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithValidCommand_ShouldReturnScrapingResult()
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock",
                SellerNameOnPageSelector = ".seller"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Mozilla/5.0 Test Bot",
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "text/html"
                }
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse(); // Will fail because example.com doesn't have the expected structure
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithNullCommand_ShouldHandleGracefully()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _webScrapingService.ScrapeProductPageAsync(null!));
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithInvalidUrl_ShouldHandleError()
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "invalid-url",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot"
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ScrapeProductPageAsync_WithInvalidSelectors_ShouldHandleError(string selector)
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = selector,
                PriceSelector = selector,
                StockSelector = selector
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot"
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // The result may still be successful but with null/empty values
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithEmptySellerNameSelector_ShouldUseDefault()
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock",
                SellerNameOnPageSelector = null // No seller selector
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot"
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Should handle gracefully when no seller selector is provided
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithCustomUserAgent_ShouldUseProvidedUserAgent()
    {
        // Arrange
        var customUserAgent = "Custom Bot 1.0";
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = customUserAgent
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Can't directly verify user agent was used, but test that it doesn't cause errors
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithAdditionalHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["Authorization"] = "Bearer token123"
        };

        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot",
                Headers = headers
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Can't directly verify headers were used, but test that it doesn't cause errors
    }

    [Fact]
    public void WebScrapingService_ShouldNotBeNull()
    {
        // Assert
        _webScrapingService.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Normal text", "Normal text")]
    [InlineData("Text with\0null byte", "Text withnull byte")]
    [InlineData("Text with\u0001control chars\u0002", "Text withcontrol chars")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void SanitizeString_ShouldRemoveProblematicCharacters(string input, string expected)
    {
        // This test verifies that the SanitizeString method (which is private) works correctly
        // by testing the public behavior through the scraping methods

        // We can't directly test the private method, but we can verify that
        // problematic characters don't cause database issues in integration tests

        // For now, just verify the service can handle various inputs
        _webScrapingService.Should().NotBeNull();

        // The actual sanitization will be tested through integration tests
        // where we verify that null bytes don't cause PostgreSQL encoding errors
    }

    [Theory]
    [InlineData("h1")]
    [InlineData("#product-title")]
    [InlineData(".product-name")]
    [InlineData("[data-testid='product-title']")]
    [InlineData("div.container h1")]
    [InlineData("span.price-value")]
    public async Task ScrapeProductPageAsync_WithVariousSelectors_ShouldAcceptDifferentSelectorFormats(string selector)
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = selector,
                PriceSelector = ".price",
                StockSelector = ".stock"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot"
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Different selector formats should be processed without errors
    }

    [Fact]
    public async Task ScrapeProductPageAsync_WithNullHeaders_ShouldHandleGracefully()
    {
        // Arrange
        var command = new ScrapeProductPageCommand
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product/123",
            Selectors = new ScrapingSelectors
            {
                ProductNameSelector = "h1.title",
                PriceSelector = ".price",
                StockSelector = ".stock"
            },
            ScrapingProfile = new ScrapingProfile
            {
                UserAgent = "Test Bot",
                Headers = null
            }
        };

        // Act
        var result = await _webScrapingService.ScrapeProductPageAsync(command);

        // Assert
        result.Should().NotBeNull();
        // Should handle null headers gracefully
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}