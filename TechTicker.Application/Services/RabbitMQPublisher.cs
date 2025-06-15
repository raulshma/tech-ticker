using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// RabbitMQ message publisher implementation
/// </summary>
public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly MessagingConfiguration _config;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMQPublisher(
        IOptions<MessagingConfiguration> config,
        ILogger<RabbitMQPublisher> logger)
    {
        _config = config.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.ConnectionString),
            UserName = _config.Username,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare exchanges
        DeclareExchangesAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = new BasicProperties
            {
                Persistent = true,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                MessageId = Guid.NewGuid().ToString(),
                Type = typeof(T).Name
            };

            await _channel.BasicPublishAsync(
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
    }

    public async Task PublishAsync<T>(T message, string queueName) where T : class
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = new BasicProperties
            {
                Persistent = true,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                MessageId = Guid.NewGuid().ToString(),
                Type = typeof(T).Name
            };

            await _channel.BasicPublishAsync(
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
    }

    private async Task DeclareExchangesAsync()
    {
        await _channel.ExchangeDeclareAsync(_config.ScrapingExchange, ExchangeType.Topic, durable: true);
        await _channel.ExchangeDeclareAsync(_config.AlertsExchange, ExchangeType.Topic, durable: true);
        await _channel.ExchangeDeclareAsync(_config.PriceDataExchange, ExchangeType.Topic, durable: true);

        // Declare queues and bind them
        await DeclareQueuesAsync();
    }

    private async Task DeclareQueuesAsync()
    {
        // Scraping queues
        await _channel.QueueDeclareAsync(_config.ScrapeCommandQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_config.ScrapeCommandQueue, _config.ScrapingExchange, _config.ScrapeCommandRoutingKey);

        await _channel.QueueDeclareAsync(_config.ScrapingResultQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_config.ScrapingResultQueue, _config.ScrapingExchange, _config.ScrapingResultRoutingKey);

        // Price data queues
        await _channel.QueueDeclareAsync(_config.RawPriceDataQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_config.RawPriceDataQueue, _config.PriceDataExchange, _config.RawPriceDataRoutingKey);

        await _channel.QueueDeclareAsync(_config.PricePointRecordedQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_config.PricePointRecordedQueue, _config.PriceDataExchange, _config.PricePointRecordedRoutingKey);

        // Alert queues
        await _channel.QueueDeclareAsync(_config.AlertTriggeredQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_config.AlertTriggeredQueue, _config.AlertsExchange, _config.AlertTriggeredRoutingKey);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
