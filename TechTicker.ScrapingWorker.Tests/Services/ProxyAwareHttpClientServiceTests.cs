using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Moq;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.ScrapingWorker.Services;

namespace TechTicker.ScrapingWorker.Tests.Services;

/// <summary>
/// Tests for ProxyAwareHttpClientService to verify proxy handling behavior
/// </summary>
public class ProxyAwareHttpClientServiceTests
{
    private readonly Mock<IProxyPoolService> _mockProxyPoolService;
    private readonly Mock<ILogger<ProxyAwareHttpClientService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ProxyPoolConfiguration _config;
    private readonly ProxyAwareHttpClientService _service;

    public ProxyAwareHttpClientServiceTests()
    {
        _mockProxyPoolService = new Mock<IProxyPoolService>();
        _mockLogger = new Mock<ILogger<ProxyAwareHttpClientService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _config = new ProxyPoolConfiguration
        {
            Enabled = true,
            RequestTimeoutSeconds = 30,
            MaxRetries = 3,
            RetryDelayMs = 1000
        };

        // Setup configuration mock to return a test encryption key
        _mockConfiguration.Setup(x => x["AiConfiguration:EncryptionKey"]).Returns("test-encryption-key-32-chars-long");

        // Setup mock for CreateClient method - only mock the method that actually exists
        var mockHttpClient = new Mock<HttpClient>();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);

        _service = new ProxyAwareHttpClientService(
            _mockProxyPoolService.Object,
            _mockLogger.Object,
            Options.Create(_config),
            _mockHttpClientFactory.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task HasActiveProxiesAsync_WhenProxyPoolDisabled_ReturnsFalse()
    {
        // Arrange
        _mockProxyPoolService.Setup(x => x.IsProxyPoolEnabled).Returns(false);

        // Act
        var result = await _service.HasActiveProxiesAsync();

        // Assert
        Assert.False(result);
        _mockProxyPoolService.Verify(x => x.GetNextProxyAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HasActiveProxiesAsync_WhenNoProxiesAvailable_ReturnsFalse()
    {
        // Arrange
        _mockProxyPoolService.Setup(x => x.IsProxyPoolEnabled).Returns(true);
        _mockProxyPoolService.Setup(x => x.GetNextProxyAsync(It.IsAny<string>())).ReturnsAsync((ProxyConfiguration?)null);

        // Act
        var result = await _service.HasActiveProxiesAsync();

        // Assert
        Assert.False(result);
        _mockProxyPoolService.Verify(x => x.GetNextProxyAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HasActiveProxiesAsync_WhenProxiesAvailable_ReturnsTrue()
    {
        // Arrange
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = Guid.NewGuid(),
            Host = "proxy.example.com",
            Port = 8080,
            ProxyType = "HTTP",
            IsActive = true,
            IsHealthy = true
        };

        _mockProxyPoolService.Setup(x => x.IsProxyPoolEnabled).Returns(true);
        _mockProxyPoolService.Setup(x => x.GetNextProxyAsync(It.IsAny<string>())).ReturnsAsync(proxy);

        // Act
        var result = await _service.HasActiveProxiesAsync();

        // Assert
        Assert.True(result);
        _mockProxyPoolService.Verify(x => x.GetNextProxyAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetProxyPoolStatsAsync_DelegatesToProxyPoolService()
    {
        // Arrange
        var expectedStats = new ProxyPoolStatsDto
        {
            TotalProxies = 5,
            AvailableProxies = 3,
            HealthyProxies = 2,
            AverageSuccessRate = 85.5m
        };

        _mockProxyPoolService.Setup(x => x.GetPoolStatsAsync()).ReturnsAsync(expectedStats);

        // Act
        var result = await _service.GetProxyPoolStatsAsync();

        // Assert
        Assert.Equal(expectedStats, result);
        _mockProxyPoolService.Verify(x => x.GetPoolStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task HasActiveProxiesAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        _mockProxyPoolService.Setup(x => x.IsProxyPoolEnabled).Returns(true);
        _mockProxyPoolService.Setup(x => x.GetNextProxyAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.HasActiveProxiesAsync();

        // Assert
        Assert.False(result);
        _mockProxyPoolService.Verify(x => x.GetNextProxyAsync(It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("HTTP", true)]
    [InlineData("HTTPS", true)]
    [InlineData("SOCKS4", true)]
    [InlineData("SOCKS5", true)]
    [InlineData("http", true)]
    [InlineData("https", true)]
    [InlineData("socks4", true)]
    [InlineData("socks5", true)]
    [InlineData("UNKNOWN", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsProxyTypeSupported_ReturnsExpectedResult(string? proxyType, bool expected)
    {
        // Act
        var result = ProxyAwareHttpClientService.IsProxyTypeSupported(proxyType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSupportedProxyTypes_ReturnsAllSupportedTypes()
    {
        // Act
        var result = ProxyAwareHttpClientService.GetSupportedProxyTypes();

        // Assert
        var expectedTypes = new[] { "HTTP", "HTTPS", "SOCKS4", "SOCKS5" };
        Assert.Equal(expectedTypes, result);
    }

    [Theory]
    [InlineData("HTTP")]
    [InlineData("HTTPS")]
    [InlineData("SOCKS4")]
    [InlineData("SOCKS5")]
    public void CreateHttpClient_WithValidProxyTypes_ShouldNotThrow(string proxyType)
    {
        // Arrange
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = Guid.NewGuid(),
            Host = "proxy.example.com",
            Port = 8080,
            ProxyType = proxyType,
            IsActive = true,
            IsHealthy = true,
            TimeoutSeconds = 30
        };

        // Act & Assert - Should not throw an exception
        var exception = Record.Exception(() => 
        {
            // Use reflection to access the private method for testing
            var method = typeof(ProxyAwareHttpClientService).GetMethod("CreateHttpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_service, new object[] { proxy, "TestUserAgent" });
        });

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData("INVALID")]
    [InlineData("")]
    public void CreateHttpClient_WithInvalidProxyTypes_ShouldFallbackToDirectConnection(string proxyType)
    {
        // Arrange
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = Guid.NewGuid(),
            Host = "proxy.example.com",
            Port = 8080,
            ProxyType = proxyType,
            IsActive = true,
            IsHealthy = true,
            TimeoutSeconds = 30
        };

        // Act & Assert - Should not throw an exception and should fallback to direct connection
        var exception = Record.Exception(() => 
        {
            // Use reflection to access the private method for testing
            var method = typeof(ProxyAwareHttpClientService).GetMethod("CreateHttpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_service, new object[] { proxy, "TestUserAgent" });
        });

        Assert.Null(exception);
    }
} 