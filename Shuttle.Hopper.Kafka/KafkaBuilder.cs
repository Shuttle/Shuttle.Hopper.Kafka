using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Kafka;

public class KafkaBuilder(IServiceCollection services)
{
    internal readonly Dictionary<string, KafkaOptions> KafkaOptions = new();

    public IServiceCollection Services { get; } = Guard.AgainstNull(services);

    public KafkaBuilder AddOptions(string name, KafkaOptions kafkaOptions)
    {
        Guard.AgainstEmpty(name);
        Guard.AgainstNull(kafkaOptions);

        KafkaOptions.Remove(name);

        KafkaOptions.Add(name, kafkaOptions);

        return this;
    }
}