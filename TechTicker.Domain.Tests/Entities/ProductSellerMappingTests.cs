using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class ProductSellerMappingTests
{
    [Fact]
    public void ProductSellerMapping_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var mapping = new ProductSellerMapping();

        // Assert
        mapping.IsActiveForScraping.Should().BeTrue();
        mapping.ConsecutiveFailureCount.Should().Be(0);
        mapping.PriceHistory.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProductSellerMapping_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var siteConfigId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var mapping = new ProductSellerMapping
        {
            CanonicalProductId = productId,
            SellerName = "Amazon",
            ExactProductUrl = "https://amazon.com/product/12345",
            SiteConfigId = siteConfigId,
            ScrapingFrequencyOverride = "PT2H", // 2 hours
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        mapping.CanonicalProductId.Should().Be(productId);
        mapping.SellerName.Should().Be("Amazon");
        mapping.ExactProductUrl.Should().Be("https://amazon.com/product/12345");
        mapping.SiteConfigId.Should().Be(siteConfigId);
        mapping.ScrapingFrequencyOverride.Should().Be("PT2H");
        mapping.CreatedAt.Should().Be(createdAt);
        mapping.UpdatedAt.Should().Be(createdAt);
        mapping.IsActiveForScraping.Should().BeTrue();
    }

    [Fact]
    public void ProductSellerMapping_WithScrapeStatus_ShouldTrackStatus()
    {
        // Arrange
        var mapping = new ProductSellerMapping();
        var lastScrapedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var nextScrapeAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        mapping.LastScrapedAt = lastScrapedAt;
        mapping.NextScrapeAt = nextScrapeAt;
        mapping.LastScrapeStatus = "SUCCESS";

        // Assert
        mapping.LastScrapedAt.Should().Be(lastScrapedAt);
        mapping.NextScrapeAt.Should().Be(nextScrapeAt);
        mapping.LastScrapeStatus.Should().Be("SUCCESS");
    }

    [Fact]
    public void ProductSellerMapping_WithScrapeError_ShouldTrackErrorDetails()
    {
        // Arrange
        var mapping = new ProductSellerMapping();

        // Act
        mapping.LastScrapeStatus = "FAILED";
        mapping.LastScrapeErrorCode = "TIMEOUT";
        mapping.ConsecutiveFailureCount = 3;

        // Assert
        mapping.LastScrapeStatus.Should().Be("FAILED");
        mapping.LastScrapeErrorCode.Should().Be("TIMEOUT");
        mapping.ConsecutiveFailureCount.Should().Be(3);
    }

    [Theory]
    [InlineData("PT1H")] // 1 hour
    [InlineData("PT30M")] // 30 minutes
    [InlineData("PT6H")] // 6 hours
    [InlineData("P1D")] // 1 day
    public void ProductSellerMapping_WithValidFrequencyOverride_ShouldAcceptISODuration(string frequency)
    {
        // Arrange & Act
        var mapping = new ProductSellerMapping
        {
            ScrapingFrequencyOverride = frequency
        };

        // Assert
        mapping.ScrapingFrequencyOverride.Should().Be(frequency);
    }

    [Fact]
    public void ProductSellerMapping_WithIds_ShouldSetIdsCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var siteConfigId = Guid.NewGuid();

        // Act
        var mapping = new ProductSellerMapping
        {
            CanonicalProductId = productId,
            SiteConfigId = siteConfigId
        };

        // Assert
        mapping.CanonicalProductId.Should().Be(productId);
        mapping.SiteConfigId.Should().Be(siteConfigId);
        mapping.PriceHistory.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProductSellerMapping_PriceHistoryCollection_ShouldAllowAddingItems()
    {
        // Arrange
        var mapping = new ProductSellerMapping();
        var priceHistory = new PriceHistory { PriceHistoryId = Guid.NewGuid() };

        // Act
        mapping.PriceHistory.Add(priceHistory);

        // Assert
        mapping.PriceHistory.Should().HaveCount(1);
        mapping.PriceHistory.Should().Contain(priceHistory);
    }

    [Fact]
    public void ProductSellerMapping_IsActiveForScraping_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var mapping = new ProductSellerMapping();

        // Assert
        mapping.IsActiveForScraping.Should().BeTrue();
    }

    [Fact]
    public void ProductSellerMapping_WithNullSiteConfiguration_ShouldBeValid()
    {
        // Arrange & Act
        var mapping = new ProductSellerMapping
        {
            SiteConfigId = null
        };

        // Assert
        mapping.SiteConfigId.Should().BeNull();
        mapping.SiteConfiguration.Should().BeNull();
    }

    [Theory]
    [InlineData("SUCCESS")]
    [InlineData("FAILED")]
    [InlineData("TIMEOUT")]
    [InlineData("PARSING_ERROR")]
    public void ProductSellerMapping_WithVariousScrapeStatuses_ShouldAcceptValidStatuses(string status)
    {
        // Arrange & Act
        var mapping = new ProductSellerMapping
        {
            LastScrapeStatus = status
        };

        // Assert
        mapping.LastScrapeStatus.Should().Be(status);
    }

    [Fact]
    public void ProductSellerMapping_ResetFailureCount_ShouldClearConsecutiveFailures()
    {
        // Arrange
        var mapping = new ProductSellerMapping
        {
            ConsecutiveFailureCount = 5,
            LastScrapeErrorCode = "TIMEOUT"
        };

        // Act
        mapping.ConsecutiveFailureCount = 0;
        mapping.LastScrapeErrorCode = null;
        mapping.LastScrapeStatus = "SUCCESS";

        // Assert
        mapping.ConsecutiveFailureCount.Should().Be(0);
        mapping.LastScrapeErrorCode.Should().BeNull();
        mapping.LastScrapeStatus.Should().Be("SUCCESS");
    }
}