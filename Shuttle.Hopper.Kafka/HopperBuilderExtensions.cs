using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Kafka;

public static class HopperBuilderExtensions
{
    extension(HopperBuilder hopperBuilder)
    {
        public IServiceCollection UseKafka(Action<KafkaBuilder>? builder = null)
        {
            var services = hopperBuilder.Services;
            var kafkaBuilder = new KafkaBuilder(services);

            builder?.Invoke(kafkaBuilder);

            services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();

            foreach (var pair in kafkaBuilder.KafkaOptions)
            {
                services.AddOptions<KafkaOptions>(pair.Key).Configure(options =>
                {
                    options.ConsumerBuilder = pair.Value.ConsumerBuilder;
                    options.ProducerBuilder = pair.Value.ProducerBuilder;
                    options.ConsumerConfig = pair.Value.ConsumerConfig;
                    options.ProducerConfig = pair.Value.ProducerConfig;

                    options.BootstrapServers = pair.Value.BootstrapServers;
                    options.MessageSendMaxRetries = pair.Value.MessageSendMaxRetries;
                    options.NumPartitions = pair.Value.NumPartitions;
                    options.ReplicationFactor = pair.Value.ReplicationFactor;
                    options.RetryBackoff = pair.Value.RetryBackoff;
                    options.EnableAutoCommit = pair.Value.EnableAutoCommit;
                    options.EnableAutoOffsetStore = pair.Value.EnableAutoOffsetStore;
                    options.FlushEnqueue = pair.Value.FlushEnqueue;
                    options.UseCancellationToken = pair.Value.UseCancellationToken;
                    options.ConsumeTimeout = pair.Value.ConsumeTimeout;
                    options.ConnectionsMaxIdle = pair.Value.ConnectionsMaxIdle;
                    options.OperationTimeout = pair.Value.OperationTimeout;

                    if (options.ConsumeTimeout < TimeSpan.FromMilliseconds(25))
                    {
                        options.ConsumeTimeout = TimeSpan.FromMilliseconds(25);
                    }

                    if (options.ConnectionsMaxIdle < TimeSpan.Zero)
                    {
                        options.ConnectionsMaxIdle = TimeSpan.Zero;
                    }

                    if (options.OperationTimeout < TimeSpan.FromMilliseconds(25))
                    {
                        options.OperationTimeout = TimeSpan.FromMilliseconds(25);
                    }
                });
            }

            services.AddSingleton<ITransportFactory, KafkaStreamFactory>();

            return services;
        }
    }
}