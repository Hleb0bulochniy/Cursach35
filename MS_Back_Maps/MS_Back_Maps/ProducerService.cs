using Confluent.Kafka;
using System.Text.Json;

namespace MS_Back_Maps
{
    public class ProducerService
    {
        private readonly IConfiguration _configuration;

        private readonly IProducer<Null, string> _producer;

        public ProducerService(IConfiguration configuration)
        {
            _configuration = configuration;

            var producerconfig = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            };

            _producer = new ProducerBuilder<Null, string>(producerconfig).Build();
        }

        public async Task ProduceAsync(string topic, string message)
        {
            var kafkamessage = new Message<Null, string> { Value = message, };

            await _producer.ProduceAsync(topic, kafkamessage);
        }

        public async Task<UserIdCheckModel?> WaitForKafkaResponseAsync(string requestId, string responseTopic, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<UserIdCheckModel>();

            using var cts = new CancellationTokenSource(timeout);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = $"ResponseConsumerGroup-{requestId}",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe(responseTopic);

            _ = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var consumeResult = consumer.Consume(cts.Token);
                        if (consumeResult != null)
                        {
                            var message = JsonSerializer.Deserialize<UserIdCheckModel>(consumeResult.Message.Value);
                            Console.WriteLine(consumeResult.Message.Value);
                            Console.WriteLine(requestId);
                            if (message?.requestId == requestId)
                            {
                                tcs.SetResult(message);
                                break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException a)
                {
                    Console.WriteLine(a.Message );
                    tcs.TrySetResult(null);
                }
                finally
                {
                    //consumer.Close();
                }
            }, cts.Token);

            return await tcs.Task;
        }
    }
}