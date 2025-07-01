using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;
using Xunit;
using FluentAssertions;
using TechTicker.Application.Services;

namespace TechTicker.Application.Tests.Services;

public class NetworkMonitorTests : IDisposable
{
    private readonly NetworkMonitor _networkMonitor;
    private readonly Mock<ILogger<NetworkMonitor>> _mockLogger;
    private readonly Mock<IPage> _mockPage;

    public NetworkMonitorTests()
    {
        _mockLogger = new Mock<ILogger<NetworkMonitor>>();
        _mockPage = new Mock<IPage>();
        _networkMonitor = new NetworkMonitor(_mockLogger.Object);
    }

    [Fact]
    public void StartMonitoring_ShouldSetIsMonitoringToTrue()
    {
        // Act
        _networkMonitor.StartMonitoring(_mockPage.Object);

        // Assert
        _networkMonitor.IsMonitoring.Should().BeTrue();
    }

    [Fact]
    public void StopMonitoring_ShouldSetIsMonitoringToFalse()
    {
        // Arrange
        _networkMonitor.StartMonitoring(_mockPage.Object);

        // Act
        _networkMonitor.StopMonitoring();

        // Assert
        _networkMonitor.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public async Task GetNetworkMetrics_ShouldReturnValidMetrics()
    {
        // Arrange
        _networkMonitor.StartMonitoring(_mockPage.Object);
        await Task.Delay(100); // Small delay to ensure time passage
        _networkMonitor.StopMonitoring();

        // Act
        var metrics = _networkMonitor.GetNetworkMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.MonitoringStartTime.Should().NotBeNull();
        metrics.MonitoringEndTime.Should().NotBeNull();
        metrics.TotalMonitoringTimeMs.Should().BeGreaterThan(0);
        metrics.RequestDetails.Should().NotBeNull();
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _networkMonitor.StartMonitoring(_mockPage.Object);
        _networkMonitor.StopMonitoring();

        // Act
        _networkMonitor.Reset();

        // Assert
        _networkMonitor.IsMonitoring.Should().BeFalse();
        var metrics = _networkMonitor.GetNetworkMetrics();
        metrics.RequestCount.Should().Be(0);
        metrics.SuccessfulRequestCount.Should().Be(0);
        metrics.FailedRequestCount.Should().Be(0);
        metrics.BytesReceived.Should().Be(0);
        metrics.BytesSent.Should().Be(0);
        metrics.RequestDetails.Should().BeEmpty();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _networkMonitor?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
