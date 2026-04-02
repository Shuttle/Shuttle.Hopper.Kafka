using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Kafka;

public class KafkaBuilder
{
    internal readonly Dictionary<string, Action<KafkaOptions>> KafkaConfigureOptions = new();

    public KafkaBuilder Configure(string name, Action<KafkaOptions> configureOptions)
    {
        Guard.AgainstEmpty(name);
        Guard.AgainstNull(configureOptions);

        KafkaConfigureOptions.Remove(name);
        KafkaConfigureOptions.Add(name, configureOptions);

        return this;
    }
}