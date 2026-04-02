using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Kafka;

public static class HopperBuilderExtensions
{
    extension(HopperBuilder hopperBuilder)
    {
        public HopperBuilder UseKafka(Action<KafkaBuilder>? builder = null)
        {
            var services = hopperBuilder.Services;
            var kafkaBuilder = new KafkaBuilder();

            builder?.Invoke(kafkaBuilder);

            services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();

            foreach (var pair in kafkaBuilder.KafkaConfigureOptions)
            {
                services.AddOptions<KafkaOptions>(pair.Key).Configure(options =>
                {
                    pair.Value(options);

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

            return hopperBuilder;
        }
    }
}