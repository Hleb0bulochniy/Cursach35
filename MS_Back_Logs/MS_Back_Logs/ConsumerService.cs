using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MS_Back_Logs.Controllers;

namespace MS_Back_Logs
{
    public class ConsumerService : BackgroundService
    {
        private readonly ILogger<ConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public ConsumerService(IConfiguration configuration, ILogger<ConsumerService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // НЕ блокируем старт хоста
            _ = Task.Run(() => RunLoopAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunLoopAsync(CancellationToken stoppingToken)
        {
            IConsumer<Ignore, string>? consumer = null;

            try
            {
                var bootstrap = _configuration["Kafka:BootstrapServers"];
                if (string.IsNullOrWhiteSpace(bootstrap))
                {
                    _logger.LogWarning("Kafka disabled in Logs service: Kafka:BootstrapServers is not set");
                    return; // тихо уходим, веб и Swagger живут
                }

                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = bootstrap,
                    GroupId = "LogsConsumerGroup",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                _logger.LogInformation("Logs Kafka ConsumerService initialized");

                consumer.Subscribe("LogUpdates");
                _logger.LogInformation("Logs Kafka subscribed to topic LogUpdates");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kafka consumer in Logs service. Kafka loop will not run.");
                consumer?.Close();
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        var message = consumeResult.Message?.Value;

                        if (string.IsNullOrWhiteSpace(message))
                            continue;

                        await ProcessMessageAsync(message, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // нормальное завершение по токену
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Kafka message in Logs service");
                        await Task.Delay(1000, stoppingToken); // не устраиваем tight loop
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Logs Kafka consumer loop. Exiting loop.");
            }
            finally
            {
                try
                {
                    consumer.Close();
                    _logger.LogInformation("Logs Kafka consumer closed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while closing Logs Kafka consumer");
                }
            }
        }

        private async Task ProcessMessageAsync(string message, CancellationToken ct)
        {
            // если надо — можешь здесь делать десериализацию в модель
            // var logModel = JsonSerializer.Deserialize<LogModel>(message);

            using var scope = _scopeFactory.CreateScope();
            var logsController = scope.ServiceProvider.GetRequiredService<LogsController>();

            try
            {
                // LogPost: Task<IActionResult> LogPost([FromBody] string kafkaMessage)
                await logsController.LogPost(message);
                _logger.LogInformation("Log message from Kafka processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling LogsController.LogPost from Kafka consumer");
            }
        }
    }
}
