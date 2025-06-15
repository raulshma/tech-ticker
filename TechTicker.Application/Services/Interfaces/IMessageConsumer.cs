namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Interface for consuming messages from RabbitMQ
/// </summary>
public interface IMessageConsumer
{
    Task StartConsumingAsync<T>(string queueName, Func<T, Task> messageHandler) where T : class;
    Task StopConsumingAsync();
}
