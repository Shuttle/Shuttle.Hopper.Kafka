using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper.Kafka.Tests;

public class KafkaConfiguration
{
    public static IServiceCollection GetServiceCollection(bool useCancellationToken = false)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddKafka(builder =>
        {
            var kafkaOptions = new KafkaOptions
            {
                BootstrapServers = "localhost:9092",
                UseCancellationToken = useCancellationToken,
                ConsumeTimeout = TimeSpan.FromSeconds(5),
                ConnectionsMaxIdle = TimeSpan.FromSeconds(5)
            };

            builder.AddOptions("local", kafkaOptions);
        });

        return services;
    }
}