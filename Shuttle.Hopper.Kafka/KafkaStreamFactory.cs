using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Kafka;

public class KafkaStreamFactory(IOptions<ServiceBusOptions> serviceBusOptions, IOptionsMonitor<KafkaOptions> kafkaOptions) : ITransportFactory
{
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private readonly IOptionsMonitor<KafkaOptions> _kafkaOptions = Guard.AgainstNull(kafkaOptions);

    public Task<ITransport> CreateAsync(Uri uri, CancellationToken cancellationToken = new CancellationToken())
    {
        var transportUri = new TransportUri(Guard.AgainstNull(uri)).SchemeInvariant(Scheme);
        var kafkaOptions = _kafkaOptions.Get(transportUri.ConfigurationName);

        if (kafkaOptions == null)
        {
            throw new InvalidOperationException(string.Format(Resources.TransportConfigurationNameException, transportUri.ConfigurationName));
        }

        return Task.FromResult<ITransport>(new KafkaStream(_serviceBusOptions, kafkaOptions, transportUri));
    }

    public string Scheme => "kafka";
}