using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// RabbitMQ message publisher implementation using Aspire RabbitMQ integration
/// </summary>
public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly MessagingConfiguration _config;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQPublisher(
        IConnection connection,
        IOptions<MessagingConfiguration> config,
        ILogger<RabbitMQPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = config.Value;
        _logger = logger;

        _channel = _connection.CreateModel();

        // Declare exchanges
        DeclareExchanges();
    }

    public Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Type = typeof(T).Name;

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}",
                typeof(T).Name, exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType} to exchange {Exchange}",
                typeof(T).Name, exchange);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T message, string queueName) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Type = typeof(T).Name;

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published message of type {MessageType} to queue {QueueName}",
                typeof(T).Name, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType} to queue {QueueName}",
                typeof(T).Name, queueName);
            throw;
        }

        return Task.CompletedTask;
    }

    private void DeclareExchanges()
    {
        _channel.ExchangeDeclare(_config.ScrapingExchange, ExchangeType.Topic, durable: true);
        _channel.ExchangeDeclare(_config.AlertsExchange, ExchangeType.Topic, durable: true);
        _channel.ExchangeDeclare(_config.PriceDataExchange, ExchangeType.Topic, durable: true);

        // Declare queues and bind them
        DeclareQueues();
    }

    private void DeclareQueues()
    {
        // Scraping queues
        _channel.QueueDeclare(_config.ScrapeCommandQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.ScrapeCommandQueue, _config.ScrapingExchange, _config.ScrapeCommandRoutingKey);

        _channel.QueueDeclare(_config.ScrapingResultQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.ScrapingResultQueue, _config.ScrapingExchange, _config.ScrapingResultRoutingKey);

        // Price data queues
        _channel.QueueDeclare(_config.RawPriceDataQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.RawPriceDataQueue, _config.PriceDataExchange, _config.RawPriceDataRoutingKey);

        _channel.QueueDeclare(_config.PricePointRecordedQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.PricePointRecordedQueue, _config.PriceDataExchange, _config.PricePointRecordedRoutingKey);

        // Alert queues
        _channel.QueueDeclare(_config.AlertTriggeredQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_config.AlertTriggeredQueue, _config.AlertsExchange, _config.AlertTriggeredRoutingKey);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
