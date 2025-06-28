using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using Xunit;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;
using TechTicker.ScrapingWorker;

namespace TechTicker.ScrapingWorker.Tests.Services;

public class WorkerTests
{
    private readonly Worker _worker;
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IMessageConsumer> _mockMessageConsumer;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly MessagingConfiguration _messagingConfig;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockMessageConsumer = new Mock<IMessageConsumer>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        _messagingConfig = new MessagingConfiguration
        {
            ScrapeCommandQueue = "scrape-commands",
            RawPriceDataQueue = "raw-price-data",
            ScrapingExchange = "scraping",
            ScrapingResultRoutingKey = "scraping.result",
            PriceDataExchange = "price-data",
            RawPriceDataRoutingKey = "raw.price.data"
        };

        var options = Options.Create(_messagingConfig);
        _worker = new Worker(
            _mockLogger.Object,
            _mockMessageConsumer.Object,
            _mockMessagePublisher.Object,
            _mockServiceScopeFactory.Object,
            options);
    }

    [Fact]
    public void Worker_ShouldNotBeNull()
    {
        // Assert
        _worker.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        var act = async () => await _worker.StartAsync(cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldStopSuccessfully()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        var act = async () => await _worker.StopAsync(cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Cancel immediately to test cancellation handling
        cancellationTokenSource.Cancel();

        // Act
        var act = async () =>
        {
            try
            {
                await _worker.StartAsync(cancellationToken);
                await Task.Delay(100, cancellationToken); // Short delay
                await _worker.StopAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Service_WithValidConfiguration_ShouldSetupConsumersCorrectly()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _worker.StartAsync(cancellationToken);

        // Assert
        // Verify that message consumer is configured for the expected queues
        _mockMessageConsumer.Verify(x => x.StartConsumingAsync<It.IsAnyType>(
            _messagingConfig.ScrapeCommandQueue,
            It.IsAny<Func<It.IsAnyType, Task>>()), Times.Once);

        _mockMessageConsumer.Verify(x => x.StartConsumingAsync<It.IsAnyType>(
            _messagingConfig.RawPriceDataQueue,
            It.IsAny<Func<It.IsAnyType, Task>>()), Times.Once);
    }

    [Fact]
    public void Service_ShouldImplementBackgroundService()
    {
        // Assert
        _worker.Should().BeAssignableTo<BackgroundService>();
    }

    [Fact]
    public async Task Service_WithMessageConsumerException_ShouldHandleGracefully()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        
        _mockMessageConsumer.Setup(x => x.StartConsumingAsync<It.IsAnyType>(
            It.IsAny<string>(),
            It.IsAny<Func<It.IsAnyType, Task>>()))
            .ThrowsAsync(new InvalidOperationException("Consumer setup failed"));

        // Act & Assert
        var act = async () => await _worker.StartAsync(cancellationToken);
        
        try
        {
            await act();
        }
        catch
        {
            // Exception is expected
        }

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _worker.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Worker_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Assert
        _worker.Should().NotBeNull();
        _worker.Should().BeAssignableTo<BackgroundService>();
    }
}