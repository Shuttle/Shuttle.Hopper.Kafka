using Confluent.Kafka;

namespace Shuttle.Hopper.Kafka;

public class KafkaOptions
{
    public const string SectionName = "Shuttle:Kafka";
    public Acks Acks { get; set; } = Acks.All;

    public string BootstrapServers { get; set; } = string.Empty;
    public TimeSpan ConnectionsMaxIdle { get; set; } = TimeSpan.Zero;
    public TimeSpan ConsumeTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableAutoCommit { get; set; }
    public bool EnableAutoOffsetStore { get; set; }
    public bool EnableIdempotence { get; set; } = true;
    public bool FlushEnqueue { get; set; }
    public int MessageSendMaxRetries { get; set; } = 3;
    public int NumPartitions { get; set; } = 1;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public short ReplicationFactor { get; set; } = 1;
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromSeconds(1);
    public bool UseCancellationToken { get; set; } = true;

    public ConsumerBuilder<Ignore, string>? ConsumerBuilder { get; set; }
    public ProducerBuilder<Null, string>? ProducerBuilder { get; set; }
    public ConsumerConfig? ConsumerConfig { get; set; }
    public ProducerConfig? ProducerConfig { get; set; }
}