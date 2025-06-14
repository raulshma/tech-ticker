using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TechTicker.ScraperService.Messages;

namespace TechTicker.ScraperService.Services
{
    /// <summary>
    /// Service for consuming messages from RabbitMQ
    /// </summary>
    public class MessageConsumerService : IMessageConsumerService, IDisposable
    {
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly IConnection _connection;
        private IModel? _channel;
        private EventingBasicConsumer? _consumer;
        private string? _consumerTag;

        public MessageConsumerService(ILogger<MessageConsumerService> logger, IConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public async Task StartConsumingAsync(Func<ScrapeProductPageCommand, Task> messageHandler, CancellationToken cancellationToken)
        {
            try
            {
                InitializeRabbitMQ();
                
                if (_channel == null || _consumer == null)
                {
                    throw new InvalidOperationException("RabbitMQ channel or consumer not initialized");
                }

                var queueName = "scrape-product-page-commands";

                _consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        
                        _logger.LogDebug("Received message: {Message}", message);

                        var command = JsonSerializer.Deserialize<ScrapeProductPageCommand>(message);
                        if (command != null)
                        {
                            await messageHandler(command);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogDebug("Message processed and acknowledged");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize message, rejecting");
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                        try
                        {
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                        catch (Exception nackEx)
                        {
                            _logger.LogError(nackEx, "Error sending NACK");
                        }
                    }
                };

                _consumerTag = _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: _consumer);

                _logger.LogInformation("Started consuming messages from queue: {QueueName}", queueName);
                
                // Keep method async for consistency but don't need to await here
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting message consumer");
                throw;
            }
        }

        public async Task StopConsumingAsync()
        {
            try
            {
                if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
                {
                    _channel.BasicCancel(_consumerTag);
                    _logger.LogInformation("Stopped consuming messages");
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping message consumer");
            }
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                _logger.LogInformation("Initializing RabbitMQ consumer");

                _channel = _connection.CreateModel();

                var queueName = "scrape-product-page-commands";

                // Declare queue (idempotent)
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Set QoS to process one message at a time
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _consumer = new EventingBasicConsumer(_channel);

                _logger.LogInformation("RabbitMQ connection initialized for queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ connection");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.Dispose();
                // Don't dispose the connection as it's managed by the DI container
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ resources");
            }
        }
    }
}
