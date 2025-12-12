using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MS_Back_Auth.Controllers; // для HelpFuncs
using MS_Back_Auth.Data;
using MS_Back_Auth.Models;

namespace MS_Back_Auth
{
    public class ConsumerService : BackgroundService
    {
        private readonly ILogger<ConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public ConsumerService(
            IConfiguration configuration,
            ILogger<ConsumerService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = Task.Run(() => RunLoopAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunLoopAsync(CancellationToken stoppingToken)
        {
            IConsumer<Ignore, string>? consumer = null;
            IProducer<Null, string>? producer = null;

            try
            {
                var bootstrap = _configuration["Kafka:BootstrapServers"];
                if (string.IsNullOrWhiteSpace(bootstrap))
                {
                    _logger.LogWarning("Kafka disabled: Kafka:BootstrapServers is not set");
                    return;
                }

                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = bootstrap,
                    GroupId = "InventoryConsumerGroup",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = bootstrap
                };

                producer = new ProducerBuilder<Null, string>(producerConfig).Build();

                _logger.LogInformation("Kafka ConsumerService initialized");
                consumer.Subscribe("UserIdCheckRequest");
                _logger.LogInformation("Kafka subscribed to topic UserIdCheckRequest");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kafka consumer/producer. Kafka loop will not run.");
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

                        await ProcessMessageAsync(message, producer, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Kafka message");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Kafka consumer loop. Exiting loop.");
            }
            finally
            {
                try
                {
                    consumer.Close();
                    _logger.LogInformation("Kafka consumer closed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while closing Kafka consumer");
                }
            }
        }

        private async Task ProcessMessageAsync(string message, IProducer<Null, string> producer, CancellationToken ct)
        {
            UserIdCheckDTO? request;
            try
            {
                request = JsonSerializer.Deserialize<UserIdCheckDTO>(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize UserIdCheckDTO from message: {Message}", message);
                return;
            }

            if (request == null)
                return;

            using var scope = _scopeFactory.CreateScope();
            var helpFuncs = scope.ServiceProvider.GetRequiredService<HelpFuncs>();
            var authContext = scope.ServiceProvider.GetRequiredService<AuthContext>();

            var response = await UserIdCheckCore(request, helpFuncs, authContext, ct);

            try
            {
                var respJson = JsonSerializer.Serialize(response);
                await producer.ProduceAsync("UserIdCheckResponce", new Message<Null, string>
                {
                    Value = respJson
                }, ct);

                _logger.LogInformation("UserIdCheckResponce sent for userId {UserId}", response.userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to produce UserIdCheckResponce to Kafka");
            }
        }
        private static async Task<UserIdCheckDTO> UserIdCheckCore(
            UserIdCheckDTO userIdCheckModel,
            HelpFuncs helpFuncs,
            AuthContext context,
            CancellationToken ct)
        {
            LogModel logModel = helpFuncs.LogModelCreate("UserIdCheck", "Check successful");

            try
            {
                if (userIdCheckModel == null || userIdCheckModel.userId < 0)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Received data is wrong";
                    logModel.ErrorCode = "400";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await helpFuncs.LogEventAsync(logModel);
                    return userIdCheckModel;
                }

                User? user = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userIdCheckModel.userId, ct);

                if (user == null)
                {
                    user = await context.Users
                        .FirstOrDefaultAsync(u => u.PlayerId == userIdCheckModel.playerId, ct);

                    if (user == null)
                    {
                        user = await context.Users
                            .FirstOrDefaultAsync(u => u.CreatorId == userIdCheckModel.creatorId, ct);
                    }
                }

                if (user == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "There is no such user";
                    logModel.ErrorCode = "404";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await helpFuncs.LogEventAsync(logModel);
                    return userIdCheckModel;
                }

                userIdCheckModel.isValid = true;
                userIdCheckModel.userName = user.Username;

                if (userIdCheckModel.requestMessage == "player")
                {
                    if (user.PlayerId == null || user.PlayerId <= 0)
                    {
                        var maxPlayerId = await context.Users
                            .Where(u => u.PlayerId != null && u.PlayerId > 0)
                            .MaxAsync(u => (int?)u.PlayerId, ct) ?? 0;

                        user.PlayerId = maxPlayerId + 1;
                        await context.SaveChangesAsync(ct);
                    }

                    userIdCheckModel.playerId = user.PlayerId!.Value;
                }

                if (userIdCheckModel.requestMessage == "creator")
                {
                    if (user.CreatorId == null || user.CreatorId <= 0)
                    {
                        var maxCreatorId = await context.Users
                            .Where(u => u.CreatorId != null && u.CreatorId > 0)
                            .MaxAsync(u => (int?)u.CreatorId, ct) ?? 0;

                        user.CreatorId = maxCreatorId + 1;
                        await context.SaveChangesAsync(ct);
                    }

                    userIdCheckModel.creatorId = user.CreatorId!.Value;
                }

                return userIdCheckModel;
            }
            catch (Exception ex)
            {
                await helpFuncs.LogModelChangeForServerError(logModel, ex);
                return userIdCheckModel;
            }
        }
    }
}
