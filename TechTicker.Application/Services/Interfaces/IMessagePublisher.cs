namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Interface for publishing messages to RabbitMQ
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string exchange, string routingKey) where T : class;
    Task PublishAsync<T>(T message, string queueName) where T : class;
}
