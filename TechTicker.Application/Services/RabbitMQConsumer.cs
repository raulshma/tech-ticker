using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// RabbitMQ message consumer implementation
/// </summary>
public class RabbitMQConsumer : IMessageConsumer, IDisposable
{
    private readonly MessagingConfiguration _config;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly List<string> _consumerTags = new();

    public RabbitMQConsumer(
        IOptions<MessagingConfiguration> config,
        ILogger<RabbitMQConsumer> logger)
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

        // Set QoS to process one message at a time
        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false).GetAwaiter().GetResult();
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, Task> messageHandler) where T : class
    {
        try
        {
            // Ensure queue exists
            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(messageJson);

                    if (message != null)
                    {
                        await messageHandler(message);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        
                        _logger.LogDebug("Successfully processed message of type {MessageType} from queue {QueueName}",
                            typeof(T).Name, queueName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue {QueueName}", queueName);
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            var consumerTag = await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _consumerTags.Add(consumerTag);

            _logger.LogInformation("Started consuming messages of type {MessageType} from queue {QueueName}",
                typeof(T).Name, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming from queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            foreach (var consumerTag in _consumerTags)
            {
                await _channel.BasicCancelAsync(consumerTag);
            }
            _consumerTags.Clear();

            _logger.LogInformation("Stopped consuming messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumption");
            throw;
        }
    }

    public void Dispose()
    {
        StopConsumingAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
