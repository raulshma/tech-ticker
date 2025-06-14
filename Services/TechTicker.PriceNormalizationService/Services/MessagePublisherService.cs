using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TechTicker.PriceNormalizationService.Messages;

namespace TechTicker.PriceNormalizationService.Services
{
    /// <summary>
    /// Service for publishing messages to RabbitMQ
    /// </summary>
    public class MessagePublisherService : IMessagePublisherService, IDisposable
    {
        private readonly ILogger<MessagePublisherService> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;

        public MessagePublisherService(ILogger<MessagePublisherService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeRabbitMQ();
        }

        public async Task PublishPricePointRecordedAsync(PricePointRecordedEvent pricePointEvent)
        {
            try
            {
                EnsureChannelInitialized();
                
                var exchange = _configuration["RabbitMQ:PricePointRecordedExchange"] ?? "price-point-recorded";
                var routingKey = _configuration["RabbitMQ:PricePointRecordedRoutingKey"] ?? "price.point.recorded";
                var message = JsonSerializer.Serialize(pricePointEvent);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>
                {
                    ["CanonicalProductId"] = pricePointEvent.CanonicalProductId.ToString(),
                    ["SellerName"] = pricePointEvent.SellerName,
                    ["StockStatus"] = pricePointEvent.StockStatus
                };

                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published price point recorded event for product {ProductId} from {Seller} at price {Price}", 
                    pricePointEvent.CanonicalProductId, pricePointEvent.SellerName, pricePointEvent.Price);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing price point recorded event for product {ProductId}", 
                    pricePointEvent.CanonicalProductId);
                throw;
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

                // Declare exchanges (idempotent)
                var pricePointRecordedExchange = _configuration["RabbitMQ:PricePointRecordedExchange"] ?? "price-point-recorded";

                _channel.ExchangeDeclare(
                    exchange: pricePointRecordedExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("RabbitMQ publisher connection initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ publisher connection");
                throw;
            }
        }

        private void EnsureChannelInitialized()
        {
            if (_channel == null)
            {
                InitializeRabbitMQ();
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
