using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TechTicker.PriceHistoryService.Messages;

namespace TechTicker.PriceHistoryService.Services
{
    /// <summary>
    /// Service for consuming price point recorded events from RabbitMQ
    /// </summary>
    public class MessageConsumerService : IMessageConsumerService, IDisposable
    {
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly string _routingKey;
        private bool _disposed = false;

        public MessageConsumerService(
            ILogger<MessageConsumerService> logger,
            IConfiguration configuration,
            IConnection connection)
        {
            _logger = logger;
            _configuration = configuration;
            _connection = connection;
            _channel = _connection.CreateModel();

            // Get configuration values
            _exchangeName = _configuration["RabbitMQ:PricePointRecordedExchange"] ?? "price-point-recorded";
            _queueName = _configuration["RabbitMQ:PriceHistoryQueue"] ?? "price-history-queue";
            _routingKey = _configuration["RabbitMQ:PricePointRecordedRoutingKey"] ?? "price.point.recorded";

            // Declare exchange and queue
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: _routingKey);

            // Set QoS to process one message at a time for reliability
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("MessageConsumerService initialized for queue {Queue} on exchange {Exchange}",
                _queueName, _exchangeName);
        }

        public Task StartConsumingAsync(Func<PricePointRecordedEvent, Task> messageHandler, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to consume messages from queue {Queue}", _queueName);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogDebug("Received message: {Message}", message);

                    var pricePointEvent = JsonSerializer.Deserialize<PricePointRecordedEvent>(message);
                    if (pricePointEvent != null)
                    {
                        await messageHandler(pricePointEvent);
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        
                        _logger.LogDebug("Successfully processed and acknowledged message for product {ProductId}",
                            pricePointEvent.CanonicalProductId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message: {Message}", message);
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization error for message: {Message}", message);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {Message}", message);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("Started consuming messages from queue {Queue}", _queueName);
            return Task.CompletedTask;
        }

        public Task StopConsumingAsync()
        {
            _logger.LogInformation("Stopping message consumption from queue {Queue}", _queueName);

            if (!_disposed)
            {
                _channel?.Close();
                _connection?.Close();
            }

            _logger.LogInformation("Stopped consuming messages from queue {Queue}", _queueName);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
