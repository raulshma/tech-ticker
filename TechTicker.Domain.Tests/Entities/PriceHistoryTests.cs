using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class PriceHistoryTests
{
    [Fact]
    public void PriceHistory_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var priceHistory = new PriceHistory();

        // Assert
        priceHistory.Should().NotBeNull();
    }

    [Fact]
    public void PriceHistory_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        // Act
        var priceHistory = new PriceHistory
        {
            Timestamp = timestamp,
            CanonicalProductId = productId,
            MappingId = mappingId,
            SellerName = "Amazon",
            Price = 299.99m,
            StockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/12345",
            ScrapedProductNameOnPage = "iPhone 15 Pro 256GB"
        };

        // Assert
        priceHistory.Timestamp.Should().Be(timestamp);
        priceHistory.CanonicalProductId.Should().Be(productId);
        priceHistory.MappingId.Should().Be(mappingId);
        priceHistory.SellerName.Should().Be("Amazon");
        priceHistory.Price.Should().Be(299.99m);
        priceHistory.StockStatus.Should().Be("In Stock");
        priceHistory.SourceUrl.Should().Be("https://amazon.com/product/12345");
        priceHistory.ScrapedProductNameOnPage.Should().Be("iPhone 15 Pro 256GB");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(9.99)]
    [InlineData(99.99)]
    [InlineData(999.99)]
    [InlineData(9999.99)]
    public void PriceHistory_WithVariousPrices_ShouldAcceptValidPrices(decimal price)
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            Price = price
        };

        // Assert
        priceHistory.Price.Should().Be(price);
    }

    [Theory]
    [InlineData("In Stock")]
    [InlineData("Out of Stock")]
    [InlineData("Limited Stock")]
    [InlineData("Temporarily Unavailable")]
    [InlineData("Discontinued")]
    public void PriceHistory_WithVariousStockStatuses_ShouldAcceptValidStatuses(string stockStatus)
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            StockStatus = stockStatus
        };

        // Assert
        priceHistory.StockStatus.Should().Be(stockStatus);
    }

    [Theory]
    [InlineData("Amazon")]
    [InlineData("Best Buy")]
    [InlineData("Walmart")]
    [InlineData("Target")]
    [InlineData("B&H Photo")]
    public void PriceHistory_WithVariousSellerNames_ShouldAcceptValidSellers(string sellerName)
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            SellerName = sellerName
        };

        // Assert
        priceHistory.SellerName.Should().Be(sellerName);
    }

    [Fact]
    public void PriceHistory_WithNullScrapedProductName_ShouldBeValid()
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            SellerName = "Amazon",
            Price = 199.99m,
            StockStatus = "In Stock",
            SourceUrl = "https://amazon.com/product/test",
            ScrapedProductNameOnPage = null
        };

        // Assert
        priceHistory.ScrapedProductNameOnPage.Should().BeNull();
        priceHistory.SellerName.Should().Be("Amazon");
        priceHistory.Price.Should().Be(199.99m);
    }

    [Fact]
    public void PriceHistory_WithIds_ShouldSetIdsCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        // Act
        var priceHistory = new PriceHistory
        {
            CanonicalProductId = productId,
            MappingId = mappingId
        };

        // Assert
        priceHistory.CanonicalProductId.Should().Be(productId);
        priceHistory.MappingId.Should().Be(mappingId);
    }

    [Fact]
    public void PriceHistory_WithLongSourceUrl_ShouldHandleLongUrls()
    {
        // Arrange
        var longUrl = "https://example.com/product/" + new string('a', 2000); // Close to 2048 limit

        // Act
        var priceHistory = new PriceHistory
        {
            SourceUrl = longUrl
        };

        // Assert
        priceHistory.SourceUrl.Should().Be(longUrl);
        priceHistory.SourceUrl.Length.Should().BeLessOrEqualTo(2048);
    }

    [Fact]
    public void PriceHistory_WithLongScrapedProductName_ShouldHandleLongNames()
    {
        // Arrange
        var longProductName = new string('X', 500); // Close to 512 limit

        // Act
        var priceHistory = new PriceHistory
        {
            ScrapedProductNameOnPage = longProductName
        };

        // Assert
        priceHistory.ScrapedProductNameOnPage.Should().Be(longProductName);
        priceHistory.ScrapedProductNameOnPage.Length.Should().BeLessOrEqualTo(512);
    }

    [Fact]
    public void PriceHistory_TimestampComparison_ShouldAllowTimeOrdering()
    {
        // Arrange
        var earlierTime = DateTimeOffset.UtcNow.AddHours(-2);
        var laterTime = DateTimeOffset.UtcNow;

        var earlierPrice = new PriceHistory { Timestamp = earlierTime, Price = 300.00m };
        var laterPrice = new PriceHistory { Timestamp = laterTime, Price = 280.00m };

        // Act & Assert
        earlierPrice.Timestamp.Should().BeBefore(laterPrice.Timestamp);
        laterPrice.Price.Should().BeLessThan(earlierPrice.Price); // Price dropped
    }

    [Fact]
    public void PriceHistory_WithEmptyScrapedProductName_ShouldBeValid()
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            ScrapedProductNameOnPage = ""
        };

        // Assert
        priceHistory.ScrapedProductNameOnPage.Should().Be("");
    }

    [Theory]
    [InlineData("https://amazon.com/dp/B123456789")]
    [InlineData("https://bestbuy.com/product/12345")]
    [InlineData("https://walmart.com/ip/item-name/123456789")]
    public void PriceHistory_WithVariousSourceUrls_ShouldAcceptValidUrls(string sourceUrl)
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            SourceUrl = sourceUrl
        };

        // Assert
        priceHistory.SourceUrl.Should().Be(sourceUrl);
    }

    [Fact]
    public void PriceHistory_PriceWithTwoDecimalPlaces_ShouldMaintainPrecision()
    {
        // Arrange & Act
        var priceHistory = new PriceHistory
        {
            Price = 123.45m
        };

        // Assert
        priceHistory.Price.Should().Be(123.45m);
        // Verify decimal precision is maintained
        (priceHistory.Price * 100).Should().Be(12345m);
    }
}