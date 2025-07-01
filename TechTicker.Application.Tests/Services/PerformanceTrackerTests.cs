using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using TechTicker.Application.Services;

namespace TechTicker.Application.Tests.Services;

public class PerformanceTrackerTests
{
    private readonly PerformanceTracker _performanceTracker;
    private readonly Mock<ILogger<PerformanceTracker>> _mockLogger;

    public PerformanceTrackerTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceTracker>>();
        _performanceTracker = new PerformanceTracker(_mockLogger.Object);
    }

    [Fact]
    public void StartNavigationTracking_ShouldSetIsTrackingToTrue()
    {
        // Act
        _performanceTracker.StartNavigationTracking();

        // Assert
        _performanceTracker.IsTracking.Should().BeTrue();
    }

    [Fact]
    public void StopNavigationTracking_ShouldSetIsTrackingToFalse()
    {
        // Arrange
        _performanceTracker.StartNavigationTracking();

        // Act
        _performanceTracker.StopNavigationTracking();

        // Assert
        _performanceTracker.IsTracking.Should().BeFalse();
    }

    [Fact]
    public async Task GetNavigationMetrics_ShouldReturnValidMetrics()
    {
        // Arrange
        _performanceTracker.StartNavigationTracking();
        await Task.Delay(100); // Small delay to ensure time passage
        _performanceTracker.RecordDomContentLoaded();
        await Task.Delay(100); // Small delay to ensure time passage
        _performanceTracker.RecordPageLoad();
        await Task.Delay(100); // Small delay to ensure time passage
        _performanceTracker.RecordNetworkIdle();
        _performanceTracker.StopNavigationTracking();

        // Act
        var metrics = _performanceTracker.GetNavigationMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.NavigationStartTime.Should().NotBeNull();
        metrics.DomContentLoadedTime.Should().NotBeNull();
        metrics.PageLoadTime.Should().NotBeNull();
        metrics.NetworkIdleTime.Should().NotBeNull();
        metrics.NavigationEndTime.Should().NotBeNull();
        
        metrics.TotalNavigationTimeMs.Should().BeGreaterThan(0);
        metrics.DomContentLoadedTimeMs.Should().BeGreaterThan(0);
        metrics.PageLoadTimeMs.Should().BeGreaterThan(0);
        metrics.NetworkIdleTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _performanceTracker.StartNavigationTracking();
        _performanceTracker.RecordDomContentLoaded();
        _performanceTracker.StopNavigationTracking();

        // Act
        _performanceTracker.Reset();

        // Assert
        _performanceTracker.IsTracking.Should().BeFalse();
        var metrics = _performanceTracker.GetNavigationMetrics();
        metrics.NavigationStartTime.Should().BeNull();
        metrics.DomContentLoadedTime.Should().BeNull();
        metrics.PageLoadTime.Should().BeNull();
        metrics.NetworkIdleTime.Should().BeNull();
        metrics.NavigationEndTime.Should().BeNull();
    }
}
