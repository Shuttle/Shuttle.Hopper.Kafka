using Microsoft.Extensions.Options;

namespace Shuttle.Hopper.Kafka;

public class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidateOptionsResult.Fail(Resources.TransportConfigurationNameException);
        }

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            return ValidateOptionsResult.Fail(string.Format(Resources.TransportConfigurationItemException, name, nameof(options.BootstrapServers)));
        }

        return ValidateOptionsResult.Success;
    }
}