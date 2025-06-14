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
        private readonly IConnection _connection;
        private IModel? _channel;

        public MessagePublisherService(ILogger<MessagePublisherService> logger, IConnection connection)
        {
            _logger = logger;
            _connection = connection;
            InitializeRabbitMQ();
        }

        public async Task PublishPricePointRecordedAsync(PricePointRecordedEvent pricePointEvent)
        {
            try
            {
                EnsureChannelInitialized();
                
                var exchange = "price-point-recorded";
                var routingKey = "price.point.recorded";
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

                _logger.LogDebug("Published price point recorded event for product {ProductId} from {Seller}", 
                    pricePointEvent.CanonicalProductId, pricePointEvent.SellerName);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing price point recorded event");
                throw;
            }
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                _logger.LogInformation("Initializing RabbitMQ publisher for price normalization");

                _channel = _connection.CreateModel();

                // Declare exchange for price point recorded events
                var exchange = "price-point-recorded";

                _channel.ExchangeDeclare(
                    exchange: exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("RabbitMQ price normalization publisher initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ price normalization publisher connection");
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
                // Don't dispose the connection as it's managed by the DI container
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ resources");
            }
        }
    }
}
