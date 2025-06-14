using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using TechTicker.ScrapingOrchestrationService.Messages;
using TechTicker.ScrapingOrchestrationService.Services;

namespace TechTicker.ScrapingOrchestrationService.Workers
{    public class ScrapingResultConsumerWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScrapingResultConsumerWorker> _logger;
        private readonly IConnection _connection;
        private IModel? _channel;

        // Queue and exchange names
        private const string ScrapingExchangeName = "techticker.scraping";
        private const string ScrapingResultQueueName = "techticker.scraping.results.orchestration";
        private const string ScrapingResultRoutingKey = "scraping.result.*";

        public ScrapingResultConsumerWorker(
            IServiceProvider serviceProvider,
            ILogger<ScrapingResultConsumerWorker> logger,
            IConnection connection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _connection = connection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping Result Consumer Worker started");

            try
            {
                await InitializeAsync();
                await ConsumeMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Scraping Result Consumer Worker");
            }
        }        private Task InitializeAsync()
        {
            _logger.LogInformation("Initializing RabbitMQ consumer");

            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: ScrapingExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: ScrapingResultQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: ScrapingResultQueueName,
                exchange: ScrapingExchangeName,
                routingKey: ScrapingResultRoutingKey);            _logger.LogInformation("RabbitMQ consumer initialized successfully");
            return Task.CompletedTask;
        }

        private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogDebug("Received scraping result message: {RoutingKey}", ea.RoutingKey);

                    var scrapingResult = JsonConvert.DeserializeObject<ScrapingResultEvent>(message);
                    if (scrapingResult != null)
                    {
                        await ProcessScrapingResultAsync(scrapingResult);
                    }

                    _channel!.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scraping result message");
                    
                    // Reject and don't requeue to prevent infinite loop
                    _channel!.BasicReject(ea.DeliveryTag, false);
                }
            };

            _channel!.BasicConsume(
                queue: ScrapingResultQueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming scraping result messages");

            // Keep the consumer running until cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scraping Result Consumer Worker stopping...");
            }
        }

        private async Task ProcessScrapingResultAsync(ScrapingResultEvent scrapingResult)
        {
            using var scope = _serviceProvider.CreateScope();
            var schedulerService = scope.ServiceProvider.GetRequiredService<IScrapingSchedulerService>();

            try
            {
                if (!scrapingResult.WasSuccessful)
                {
                    _logger.LogWarning("Scraping failed for mapping {MappingId}: {ErrorCode} - {ErrorMessage}", 
                        scrapingResult.MappingId, scrapingResult.ErrorCode, scrapingResult.ErrorMessage);

                    // Handle failed scraping - adjust schedule based on error type
                    await HandleFailedScrapingAsync(scrapingResult, schedulerService);
                }
                else
                {
                    _logger.LogDebug("Scraping succeeded for mapping {MappingId}", scrapingResult.MappingId);
                    
                    // For successful scraping, the schedule was already updated when the command was sent
                    // No additional action needed here
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scraping result for mapping {MappingId}", scrapingResult.MappingId);
            }
        }

        private async Task HandleFailedScrapingAsync(ScrapingResultEvent scrapingResult, IScrapingSchedulerService schedulerService)
        {
            // Determine retry strategy based on error code
            var retryDelay = scrapingResult.ErrorCode switch
            {
                "BLOCKED_BY_CAPTCHA" => TimeSpan.FromHours(2), // Wait longer for CAPTCHA blocks
                "HTTP_ERROR" when scrapingResult.HttpStatusCode == 429 => TimeSpan.FromHours(1), // Rate limited
                "HTTP_ERROR" when scrapingResult.HttpStatusCode >= 500 => TimeSpan.FromMinutes(30), // Server error
                "PARSING_ERROR" => TimeSpan.FromMinutes(15), // Quick retry for parsing issues
                _ => TimeSpan.FromMinutes(30) // Default retry delay
            };

            var nextRetryTime = scrapingResult.Timestamp.Add(retryDelay);
            
            await schedulerService.UpdateMappingScheduleAsync(
                scrapingResult.MappingId,
                lastScrapedAt: null, // Don't update LastScrapedAt for failed attempts
                nextScrapeAt: nextRetryTime);

            _logger.LogInformation("Scheduled retry for mapping {MappingId} at {RetryTime} due to error: {ErrorCode}", 
                scrapingResult.MappingId, nextRetryTime, scrapingResult.ErrorCode);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Scraping Result Consumer Worker is stopping");
            
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while closing RabbitMQ connections");
            }

            await base.StopAsync(cancellationToken);
        }        public override void Dispose()
        {
            try
            {
                _channel?.Dispose();
                // Don't dispose the connection as it's managed by the DI container
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while disposing RabbitMQ resources");
            }

            base.Dispose();
        }
    }
}
