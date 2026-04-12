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

            builder?.Invoke(new(services));

            services.PostConfigureAll<KafkaOptions>(options =>
            {
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

            services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();
            services.AddSingleton<ITransportFactory, KafkaStreamFactory>();

            return hopperBuilder;
        }
    }
}