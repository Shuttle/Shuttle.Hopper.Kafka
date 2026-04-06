using NUnit.Framework;
using Shuttle.Hopper.Testing;

namespace Shuttle.Hopper.Kafka.Tests;

public class KafkaInboxFixture : InboxFixture
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_handle_errors_async(bool hasErrorQueue)
    {
        await TestInboxErrorAsync(KafkaConfiguration.GetServiceCollection(), "kafka://local/{0}", hasErrorQueue);
    }

    public async Task Should_be_able_to_process_queue_timeously_async()
    {
        await TestInboxThroughputAsync(KafkaConfiguration.GetServiceCollection(true), "kafka://local/{0}", 1000, 5, TimeSpan.FromMinutes(1));
    }
}