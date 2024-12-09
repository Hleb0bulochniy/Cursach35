using Confluent.Kafka;
using MS_Back_Auth.Controllers;
using MS_Back_Auth.Data;
using System.Text.Json;

namespace MS_Back_Auth
{
    public class ConsumerService : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<ConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly AuthController _controller;

        public ConsumerService(IConfiguration configuration, ILogger<ConsumerService> logger, AuthController authController)
        {
            _logger = logger;
            _configuration = configuration;
            _controller = authController;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = "InventoryConsumerGroup",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe("UserIdCheckRequest");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                await ProcessMessageAsync(consumeResult.Message.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Kafka message: {ex.Message}");
            }
            }

            _consumer.Close();
        }

        private async Task ProcessMessageAsync(string message)
        {
            var request = JsonSerializer.Deserialize<UserIdCheckModel>(message);
            if (request == null) return;

            // Логика проверки существования пользователя
            bool userExists = await _controller.UserIdCheck(request.userId);

            // Создание ответа
            UserIdCheckModel response = new UserIdCheckModel
            {
                requestId = request.requestId,
                userId = request.userId,
                isValid = userExists
            };

            // Отправка ответа в Kafka
            await _producer.ProduceAsync("UserIdCheckResponce", new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(response)
            });
        }
    }
}
