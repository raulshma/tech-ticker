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
/// RabbitMQ message consumer implementation using Aspire RabbitMQ integration
/// </summary>
public class RabbitMQConsumer : IMessageConsumer, IDisposable
{
    private readonly MessagingConfiguration _config;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly List<string> _consumerTags = new();

    public RabbitMQConsumer(
        IConnection connection,
        IOptions<MessagingConfiguration> config,
        ILogger<RabbitMQConsumer> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = config.Value;
        _logger = logger;

        _channel = _connection.CreateModel();

        // Set QoS to process one message at a time
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    public Task StartConsumingAsync<T>(string queueName, Func<T, Task> messageHandler) where T : class
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

        if (messageHandler == null)
            throw new ArgumentNullException(nameof(messageHandler));

        try
        {
            // Ensure queue exists
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(messageJson);

                    if (message != null)
                    {
                        await messageHandler(message);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);

                        _logger.LogDebug("Successfully processed message of type {MessageType} from queue {QueueName}",
                            typeof(T).Name, queueName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue {QueueName}", queueName);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            var consumerTag = _channel.BasicConsume(
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

        return Task.CompletedTask;
    }

    public Task StopConsumingAsync()
    {
        try
        {
            foreach (var consumerTag in _consumerTags)
            {
                _channel.BasicCancel(consumerTag);
            }
            _consumerTags.Clear();

            _logger.LogInformation("Stopped consuming messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumption");
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopConsumingAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
