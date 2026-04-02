using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Hopper.Kafka.Tests;

public class KafkaConfiguration
{
    public static IServiceCollection GetServiceCollection(bool useCancellationToken = false)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddHopper()
            .UseKafka(builder =>
            {
                builder.Configure("local", options =>
                {
                    options.BootstrapServers = "localhost:9092";
                    options.UseCancellationToken = useCancellationToken;
                    options.ConsumeTimeout = TimeSpan.FromSeconds(5);
                    options.ConnectionsMaxIdle = TimeSpan.FromSeconds(5);
                });
            });

        return services;
    }
}