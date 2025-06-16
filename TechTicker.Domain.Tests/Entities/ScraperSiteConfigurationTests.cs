using System.Text.Json;
using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class ScraperSiteConfigurationTests
{
    [Fact]
    public void ScraperSiteConfiguration_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration();

        // Assert
        config.IsEnabled.Should().BeTrue();
        config.ProductSellerMappings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ScraperSiteConfiguration_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var config = new ScraperSiteConfiguration
        {
            SiteDomain = "amazon.com",
            ProductNameSelector = "#productTitle",
            PriceSelector = ".a-price-whole",
            StockSelector = "#availability span",
            SellerNameOnPageSelector = "#bylineInfo",
            DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        config.SiteDomain.Should().Be("amazon.com");
        config.ProductNameSelector.Should().Be("#productTitle");
        config.PriceSelector.Should().Be(".a-price-whole");
        config.StockSelector.Should().Be("#availability span");
        config.SellerNameOnPageSelector.Should().Be("#bylineInfo");
        config.DefaultUserAgent.Should().Be("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        config.CreatedAt.Should().Be(createdAt);
        config.UpdatedAt.Should().Be(createdAt);
        config.IsEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("amazon.com")]
    [InlineData("bestbuy.com")]
    [InlineData("walmart.com")]
    [InlineData("target.com")]
    [InlineData("ebay.com")]
    public void ScraperSiteConfiguration_WithVariousDomains_ShouldAcceptValidDomains(string domain)
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration
        {
            SiteDomain = domain
        };

        // Assert
        config.SiteDomain.Should().Be(domain);
    }

    [Theory]
    [InlineData("#productTitle")]
    [InlineData(".product-name")]
    [InlineData("h1.title")]
    [InlineData("[data-testid='product-title']")]
    public void ScraperSiteConfiguration_WithVariousSelectors_ShouldAcceptValidSelectors(string selector)
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration
        {
            ProductNameSelector = selector,
            PriceSelector = selector,
            StockSelector = selector
        };

        // Assert
        config.ProductNameSelector.Should().Be(selector);
        config.PriceSelector.Should().Be(selector);
        config.StockSelector.Should().Be(selector);
    }

    [Fact]
    public void AdditionalHeadersDict_WithValidJson_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var config = new ScraperSiteConfiguration();
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "custom-value"
        };

        // Act
        config.AdditionalHeadersDict = headers;
        var retrievedHeaders = config.AdditionalHeadersDict;

        // Assert
        config.AdditionalHeaders.Should().NotBeNullOrEmpty();
        retrievedHeaders.Should().NotBeNull();
        retrievedHeaders.Should().ContainKey("Accept");
        retrievedHeaders.Should().ContainKey("Authorization");
        retrievedHeaders.Should().ContainKey("X-Custom-Header");
        retrievedHeaders!["Accept"].Should().Be("application/json");
    }

    [Fact]
    public void AdditionalHeadersDict_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var config = new ScraperSiteConfiguration();

        // Act
        config.AdditionalHeadersDict = null;

        // Assert
        config.AdditionalHeaders.Should().BeNull();
        config.AdditionalHeadersDict.Should().BeNull();
    }

    [Fact]
    public void AdditionalHeadersDict_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        var config = new ScraperSiteConfiguration { AdditionalHeaders = "" };

        // Act
        var headers = config.AdditionalHeadersDict;

        // Assert
        headers.Should().BeNull();
    }

    [Fact]
    public void AdditionalHeadersDict_WithValidJsonString_ShouldDeserialize()
    {
        // Arrange
        var jsonString = """{"User-Agent": "Custom Bot", "Referer": "https://google.com"}""";
        var config = new ScraperSiteConfiguration { AdditionalHeaders = jsonString };

        // Act
        var headers = config.AdditionalHeadersDict;

        // Assert
        headers.Should().NotBeNull();
        headers.Should().ContainKey("User-Agent");
        headers.Should().ContainKey("Referer");
        headers!["User-Agent"].Should().Be("Custom Bot");
        headers["Referer"].Should().Be("https://google.com");
    }

    [Fact]
    public void ScraperSiteConfiguration_WithNullOptionalFields_ShouldBeValid()
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration
        {
            SiteDomain = "example.com",
            ProductNameSelector = ".title",
            PriceSelector = ".price",
            StockSelector = ".stock",
            SellerNameOnPageSelector = null,
            DefaultUserAgent = null,
            AdditionalHeaders = null
        };

        // Assert
        config.SellerNameOnPageSelector.Should().BeNull();
        config.DefaultUserAgent.Should().BeNull();
        config.AdditionalHeaders.Should().BeNull();
        config.SiteDomain.Should().Be("example.com");
    }

    [Fact]
    public void ScraperSiteConfiguration_ProductSellerMappingsCollection_ShouldAllowAddingMappings()
    {
        // Arrange
        var config = new ScraperSiteConfiguration();
        var mapping1 = new ProductSellerMapping { MappingId = Guid.NewGuid() };
        var mapping2 = new ProductSellerMapping { MappingId = Guid.NewGuid() };

        // Act
        config.ProductSellerMappings.Add(mapping1);
        config.ProductSellerMappings.Add(mapping2);

        // Assert
        config.ProductSellerMappings.Should().HaveCount(2);
        config.ProductSellerMappings.Should().Contain(mapping1);
        config.ProductSellerMappings.Should().Contain(mapping2);
    }

    [Fact]
    public void ScraperSiteConfiguration_IsEnabled_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration();

        // Assert
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ScraperSiteConfiguration_CanBeDisabled_ShouldAllowSettingDisabled()
    {
        // Arrange
        var config = new ScraperSiteConfiguration();

        // Act
        config.IsEnabled = false;

        // Assert
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ScraperSiteConfiguration_CreatedAndUpdatedAt_ShouldTrackTimestamps()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var config = new ScraperSiteConfiguration
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        config.CreatedAt.Should().Be(createdAt);
        config.UpdatedAt.Should().Be(updatedAt);
        config.UpdatedAt.Should().BeAfter(config.CreatedAt);
    }

    [Fact]
    public void ScraperSiteConfiguration_ProductSellerMappingsCollection_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration();

        // Assert
        config.ProductSellerMappings.Should().NotBeNull();
        config.ProductSellerMappings.Should().BeOfType<List<ProductSellerMapping>>();
        config.ProductSellerMappings.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36")]
    [InlineData("Custom Bot 1.0")]
    public void ScraperSiteConfiguration_WithVariousUserAgents_ShouldAcceptValidUserAgents(string userAgent)
    {
        // Arrange & Act
        var config = new ScraperSiteConfiguration
        {
            DefaultUserAgent = userAgent
        };

        // Assert
        config.DefaultUserAgent.Should().Be(userAgent);
    }

    [Fact]
    public void ScraperSiteConfiguration_WithComplexAdditionalHeaders_ShouldHandleMultipleHeaders()
    {
        // Arrange
        var config = new ScraperSiteConfiguration();
        var complexHeaders = new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Accept-Language"] = "en-US,en;q=0.5",
            ["Accept-Encoding"] = "gzip, deflate",
            ["Connection"] = "keep-alive",
            ["Upgrade-Insecure-Requests"] = "1"
        };

        // Act
        config.AdditionalHeadersDict = complexHeaders;
        var retrieved = config.AdditionalHeadersDict;

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().HaveCount(5);
        retrieved.Should().ContainKeys("Accept", "Accept-Language", "Accept-Encoding", "Connection", "Upgrade-Insecure-Requests");
    }
}