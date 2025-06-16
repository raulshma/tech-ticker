using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services;

namespace TechTicker.Application.Tests.Services;

public class RabbitMQConsumerTests
{
    private readonly RabbitMQConsumer _consumer;
    private readonly Mock<ILogger<RabbitMQConsumer>> _mockLogger;
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IModel> _mockChannel;
    private readonly MessagingConfiguration _messagingConfig;

    public RabbitMQConsumerTests()
    {
        _mockLogger = new Mock<ILogger<RabbitMQConsumer>>();
        _mockConnection = new Mock<IConnection>();
        _mockChannel = new Mock<IModel>();
        
        _mockConnection.Setup(x => x.CreateModel()).Returns(_mockChannel.Object);
        
        _messagingConfig = new MessagingConfiguration
        {
            ScrapeCommandQueue = "scrape-commands",
            RawPriceDataQueue = "raw-price-data",
            AlertTriggeredQueue = "alert-triggered"
        };

        var options = Options.Create(_messagingConfig);
        _consumer = new RabbitMQConsumer(_mockConnection.Object, options, _mockLogger.Object);
    }

    [Fact]
    public void RabbitMQConsumer_ShouldNotBeNull()
    {
        // Assert
        _consumer.Should().NotBeNull();
    }

    [Fact]
    public async Task StartConsumingAsync_WithValidQueue_ShouldSetupConsumer()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<string, Task>(msg => Task.CompletedTask);

        // Act
        var act = async () => await _consumer.StartConsumingAsync(queueName, handler);

        // Assert
        await act.Should().NotThrowAsync();
        
        // Verify that the channel was used to setup basic consume
        _mockChannel.Verify(x => x.BasicConsume(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<IBasicConsumer>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task StartConsumingAsync_WithInvalidQueue_ShouldHandleGracefully(string? queueName)
    {
        // Arrange
        var handler = new Func<string, Task>(msg => Task.CompletedTask);

        // Act & Assert
        var act = async () => await _consumer.StartConsumingAsync(queueName!, handler);
        
        try
        {
            await act();
        }
        catch (ArgumentException)
        {
            // Expected for invalid queue name
        }
    }

    [Fact]
    public async Task StartConsumingAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var queueName = "test-queue";

        // Act & Assert
        var act = async () => await _consumer.StartConsumingAsync<string>(queueName, null!);
        
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StopConsumingAsync_ShouldStopGracefully()
    {
        // Act
        var act = async () => await _consumer.StopConsumingAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartConsumingAsync_MultipleQueues_ShouldHandleMultipleConsumers()
    {
        // Arrange
        var queue1 = "queue1";
        var queue2 = "queue2";
        var handler1 = new Func<string, Task>(msg => Task.CompletedTask);
        var handler2 = new Func<string, Task>(msg => Task.CompletedTask);

        // Act
        await _consumer.StartConsumingAsync(queue1, handler1);
        await _consumer.StartConsumingAsync(queue2, handler2);

        // Assert
        // Verify that BasicConsume was called twice
        _mockChannel.Verify(x => x.BasicConsume(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<IBasicConsumer>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StartConsumingAsync_WithChannelException_ShouldLogError()
    {
        // Arrange
        var queueName = "test-queue";
        var handler = new Func<string, Task>(msg => Task.CompletedTask);
        
        _mockChannel.Setup(x => x.BasicConsume(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<IBasicConsumer>()))
            .Throws(new InvalidOperationException("Channel error"));

        // Act & Assert
        var act = async () => await _consumer.StartConsumingAsync(queueName, handler);
        
        try
        {
            await act();
        }
        catch
        {
            // Expected when channel throws
        }

        // Verify error was logged
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
        var act = () => _consumer.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Consumer_WithGenericType_ShouldHandleTypedMessages()
    {
        // Arrange
        var queueName = "typed-queue";
        var handler = new Func<TestMessage, Task>(msg => Task.CompletedTask);

        // Act
        var act = async () => await _consumer.StartConsumingAsync(queueName, handler);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consumer_WithComplexGenericType_ShouldHandleComplexTypes()
    {
        // Arrange
        var queueName = "complex-queue";
        var handler = new Func<ComplexTestMessage, Task>(msg => Task.CompletedTask);

        // Act
        var act = async () => await _consumer.StartConsumingAsync(queueName, handler);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Consumer_ShouldImplementIDisposable()
    {
        // Assert
        _consumer.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public async Task StopConsumingAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await _consumer.StopConsumingAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Consumer_WithLongRunningHandler_ShouldHandleCorrectly()
    {
        // Arrange
        var queueName = "long-running-queue";
        var handler = new Func<string, Task>(async msg => 
        {
            await Task.Delay(100); // Simulate longer processing
        });

        // Act
        var act = async () => await _consumer.StartConsumingAsync(queueName, handler);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // Test helper classes
    private class TestMessage
    {
        public string Content { get; set; } = "";
        public int Id { get; set; }
    }

    private class ComplexTestMessage
    {
        public Guid Id { get; set; }
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}