using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TechTicker.ScraperService.Messages;

namespace TechTicker.ScraperService.Services
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

        public async Task PublishRawPriceDataAsync(RawPriceDataEvent priceData)
        {
            try
            {
                EnsureChannelInitialized();
                
                var exchange = _configuration["RabbitMQ:RawPriceDataExchange"] ?? "raw-price-data";
                var message = JsonSerializer.Serialize(priceData);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: "raw.price.data",
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published raw price data for product {ProductId} from {Seller}", 
                    priceData.CanonicalProductId, priceData.SellerName);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing raw price data");
                throw;
            }
        }

        public async Task PublishScrapingResultAsync(ScrapingResultEvent result)
        {
            try
            {
                EnsureChannelInitialized();
                
                var exchange = _configuration["RabbitMQ:ScrapingResultExchange"] ?? "scraping-results";
                var message = JsonSerializer.Serialize(result);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: "scraping.result",
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published scraping result for mapping {MappingId}, success: {Success}", 
                    result.MappingId, result.WasSuccessful);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing scraping result");
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
                var rawPriceDataExchange = _configuration["RabbitMQ:RawPriceDataExchange"] ?? "raw-price-data";
                var scrapingResultExchange = _configuration["RabbitMQ:ScrapingResultExchange"] ?? "scraping-results";

                _channel.ExchangeDeclare(
                    exchange: rawPriceDataExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _channel.ExchangeDeclare(
                    exchange: scrapingResultExchange,
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
