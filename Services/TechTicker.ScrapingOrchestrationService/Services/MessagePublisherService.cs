using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Text;
using TechTicker.ScrapingOrchestrationService.Messages;

namespace TechTicker.ScrapingOrchestrationService.Services
{
    public interface IMessagePublisherService
    {
        Task PublishScrapeCommandAsync(ScrapeProductPageCommand command);
        Task InitializeAsync();
        void Dispose();
    }    public class MessagePublisherService : IMessagePublisherService, IDisposable
    {
        private readonly ILogger<MessagePublisherService> _logger;
        private readonly IConnection _connection;
        private IModel? _channel;
        private bool _disposed = false;

        // Queue and exchange names
        private const string ScrapingExchangeName = "techticker.scraping";
        private const string ScrapeCommandQueueName = "techticker.scraping.commands";
        private const string ScrapeCommandRoutingKey = "scrape.product.page";

        public MessagePublisherService(
            ILogger<MessagePublisherService> logger,
            IConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }        public Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing RabbitMQ messaging");

                _channel = _connection.CreateModel();

                // Declare exchange
                _channel.ExchangeDeclare(
                    exchange: ScrapingExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // Declare queue
                _channel.QueueDeclare(
                    queue: ScrapeCommandQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind queue to exchange
                _channel.QueueBind(
                    queue: ScrapeCommandQueueName,
                    exchange: ScrapingExchangeName,
                    routingKey: ScrapeCommandRoutingKey);                _logger.LogInformation("RabbitMQ messaging initialized successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ messaging");
                throw;
            }
        }

        public async Task PublishScrapeCommandAsync(ScrapeProductPageCommand command)
        {
            if (_channel == null)
            {
                await InitializeAsync();
            }

            try
            {
                var message = JsonConvert.SerializeObject(command, Formatting.None);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>
                {
                    ["MappingId"] = command.MappingId.ToString(),
                    ["CanonicalProductId"] = command.CanonicalProductId.ToString(),
                    ["SellerName"] = command.SellerName
                };

                _channel.BasicPublish(
                    exchange: ScrapingExchangeName,
                    routingKey: ScrapeCommandRoutingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published scrape command for mapping {MappingId} to product {ProductId} at {Url}", 
                    command.MappingId, command.CanonicalProductId, command.ExactProductUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish scrape command for mapping {MappingId}", command.MappingId);
                throw;
            }
        }        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.Close();
                _channel?.Dispose();
                // Don't dispose the connection as it's managed by the DI container
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while disposing RabbitMQ resources");
            }

            _disposed = true;
        }
    }
}
