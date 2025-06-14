using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TechTicker.PriceNormalizationService.Messages;

namespace TechTicker.PriceNormalizationService.Services
{
    /// <summary>
    /// Service for consuming messages from RabbitMQ
    /// </summary>
    public class MessageConsumerService : IMessageConsumerService, IDisposable
    {
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;
        private EventingBasicConsumer? _consumer;
        private string? _consumerTag;

        public MessageConsumerService(ILogger<MessageConsumerService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartConsumingAsync(Func<RawPriceDataEvent, Task> messageHandler, CancellationToken cancellationToken)
        {
            try
            {
                InitializeRabbitMQ();
                
                if (_channel == null || _consumer == null)
                {
                    throw new InvalidOperationException("RabbitMQ channel or consumer not initialized");
                }

                var queueName = _configuration["RabbitMQ:RawPriceDataQueue"] ?? "raw-price-data-queue";

                _consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        
                        _logger.LogDebug("Received raw price data message: {MessageSize} bytes", body.Length);

                        var rawPriceData = JsonSerializer.Deserialize<RawPriceDataEvent>(message);
                        if (rawPriceData != null)
                        {
                            await messageHandler(rawPriceData);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogDebug("Raw price data message processed and acknowledged for product {ProductId}", 
                                rawPriceData.CanonicalProductId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize raw price data message, rejecting");
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing raw price data message");
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

                _logger.LogInformation("Started consuming raw price data messages from queue: {QueueName}", queueName);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting raw price data message consumer");
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
                    _logger.LogInformation("Stopped consuming raw price data messages");
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping raw price data message consumer");
            }
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var connectionString = _configuration["RabbitMQ:ConnectionString"] ?? "amqp://localhost:5672";
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(connectionString)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                var exchangeName = _configuration["RabbitMQ:RawPriceDataExchange"] ?? "raw-price-data";
                var queueName = _configuration["RabbitMQ:RawPriceDataQueue"] ?? "raw-price-data-queue";
                var routingKey = _configuration["RabbitMQ:RawPriceDataRoutingKey"] ?? "raw.price.data";

                // Declare exchange (idempotent)
                _channel.ExchangeDeclare(
                    exchange: exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // Declare queue (idempotent)
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind queue to exchange
                _channel.QueueBind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey);

                // Set QoS to process one message at a time
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _consumer = new EventingBasicConsumer(_channel);

                _logger.LogInformation("RabbitMQ connection initialized for consuming from queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ connection for consuming");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ resources");
            }
        }
    }
}
