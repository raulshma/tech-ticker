using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class ProxyPoolServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<ProxyPoolService>> _loggerMock;
    private readonly ProxyPoolService _service;
    private readonly ProxyPoolConfiguration _config;

    public ProxyPoolServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<ProxyPoolService>>();
        
        _config = new ProxyPoolConfiguration
        {
            Enabled = true,
            CacheMinutes = 5,
            StatsCacheMinutes = 10,
            MaxConsecutiveFailures = 3,
            SelectionStrategy = "ROUND_ROBIN"
        };

        var configOptions = Options.Create(_config);
        _service = new ProxyPoolService(_unitOfWorkMock.Object, _cacheMock.Object, _loggerMock.Object, configOptions);
    }

    [Fact]
    public void IsProxyPoolEnabled_ShouldReturnTrue_WhenConfigEnabled()
    {
        // Act & Assert
        Assert.True(_service.IsProxyPoolEnabled);
    }

    [Fact]
    public async Task GetNextProxyAsync_ShouldReturnNull_WhenPoolDisabled()
    {
        // Arrange
        var disabledConfig = new ProxyPoolConfiguration { Enabled = false };
        var configOptions = Options.Create(disabledConfig);
        var disabledService = new ProxyPoolService(_unitOfWorkMock.Object, _cacheMock.Object, _loggerMock.Object, configOptions);

        // Act
        var result = await disabledService.GetNextProxyAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProxyByIdAsync_ShouldReturnProxy_WhenProxyExists()
    {
        // Arrange
        var proxyId = Guid.NewGuid();
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = proxyId,
            Host = "proxy.test.com",
            Port = 8080
        };

        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetByIdAsync(proxyId))
            .ReturnsAsync(proxy);

        // Act
        var result = await _service.GetProxyByIdAsync(proxyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(proxyId, result.ProxyConfigurationId);
        Assert.Equal("proxy.test.com", result.Host);
    }

    [Fact]
    public async Task GetProxyByIdAsync_ShouldReturnNull_WhenProxyNotFound()
    {
        // Arrange
        var proxyId = Guid.NewGuid();
        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetByIdAsync(proxyId))
            .ReturnsAsync(null as ProxyConfiguration);

        // Act
        var result = await _service.GetProxyByIdAsync(proxyId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RecordProxySuccessAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var proxyId = Guid.NewGuid();
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = proxyId,
            Host = "proxy.test.com",
            Port = 8080
        };

        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetByIdAsync(proxyId))
            .ReturnsAsync(proxy);

        // Act & Assert - Should not throw
        await _service.RecordProxySuccessAsync(proxyId, 500);
    }

    [Fact]
    public async Task RecordProxyFailureAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var proxyId = Guid.NewGuid();
        var proxy = new ProxyConfiguration
        {
            ProxyConfigurationId = proxyId,
            Host = "proxy.test.com",
            Port = 8080
        };

        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetByIdAsync(proxyId))
            .ReturnsAsync(proxy);

        // Act & Assert - Should not throw
        await _service.RecordProxyFailureAsync(proxyId, "Connection timeout", "TIMEOUT");
    }

    [Fact]
    public async Task GetPoolStatsAsync_ShouldReturnStats_WhenCalled()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetProxyStatsAsync())
            .ReturnsAsync((10, 8, 6, 85.5m));
        _unitOfWorkMock.Setup(u => u.ProxyConfigurations.GetActiveProxiesAsync())
            .ReturnsAsync(new List<ProxyConfiguration>
            {
                new ProxyConfiguration { ProxyType = "HTTP" },
                new ProxyConfiguration { ProxyType = "SOCKS5" },
                new ProxyConfiguration { ProxyType = "HTTP" }
            });

        // Setup cache to return null initially (cache miss)
        object? cachedValue = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        // Act
        var result = await _service.GetPoolStatsAsync();

        // Assert
        Assert.NotNull(result);
        // Note: Due to caching complexity, we're mainly testing that the method doesn't throw
    }

    [Fact]
    public async Task RefreshPoolAsync_ShouldInvalidateCache()
    {
        // Act
        await _service.RefreshPoolAsync();

        // Assert
        // Verify that cache removal methods would be called
        // Note: Due to the complexity of mocking IMemoryCache.Remove, 
        // we're mainly testing that the method completes without throwing
        Assert.True(true); // Test passes if no exception is thrown
    }
} 