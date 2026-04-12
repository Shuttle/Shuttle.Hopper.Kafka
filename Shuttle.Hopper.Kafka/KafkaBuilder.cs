using Microsoft.Extensions.DependencyInjection;
using Shuttle.Contract;

namespace Shuttle.Hopper.Kafka;

public class KafkaBuilder(IServiceCollection services)
{
    internal readonly Dictionary<string, Action<KafkaOptions>> KafkaConfigureOptions = new();

    public KafkaBuilder Configure(string name, Action<KafkaOptions> configureOptions)
    {
        Guard.AgainstNull(services)
            .AddOptions<KafkaOptions>(Guard.AgainstEmpty(name))
            .Configure(Guard.AgainstNull(configureOptions));

        return this;
    }
}