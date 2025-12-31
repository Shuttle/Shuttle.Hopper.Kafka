using NUnit.Framework;
using Shuttle.Hopper.Testing;

namespace Shuttle.Hopper.Kafka.Tests;

public class KafkaInboxFixture : InboxFixture
{
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task Should_be_able_handle_errors_async(bool hasErrorQueue, bool isTransactionalEndpoint)
    {
        await TestInboxErrorAsync(KafkaConfiguration.GetServiceCollection(), "kafka://local/{0}", hasErrorQueue, isTransactionalEndpoint);
    }

    [TestCase(50, true)]
    [TestCase(50, false)]
    public async Task Should_be_able_to_process_queue_timeously_async(int count, bool isTransactionalEndpoint)
    {
        await TestInboxThroughputAsync(KafkaConfiguration.GetServiceCollection(true), "kafka://local/{0}", 1000, count, 1, isTransactionalEndpoint);
    }
}