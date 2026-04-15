# Shuttle.Hopper.Kafka

Kafka implementation for Shuttle.Hopper.

## Installation

```bash
dotnet add package Shuttle.Hopper.Kafka
```

## Docker Compose

To get a local Kafka broker up and running using the `docker-compose.yml` file in the root of the repository:

```bash
docker compose up -d
```

## Configuration

The URI structure is `kafka://configuration-name/topic-name`.

```c#
services.AddHopper()
    .UseKafka(builder =>
    {
        builder.Configure("local", options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.ReplicationFactor = 1;
            options.NumPartitions = 1;
            options.MessageSendMaxRetries = 3;
            options.RetryBackoff = TimeSpan.FromSeconds(1);
            options.EnableAutoCommit = false;
            options.EnableAutoOffsetStore = false;
            options.FlushEnqueue = false;
            options.UseCancellationToken = true;
            options.ConsumeTimeout = TimeSpan.FromSeconds(30);
            options.OperationTimeout = TimeSpan.FromSeconds(30);
            options.ConnectionsMaxIdle = TimeSpan.Zero;
            options.Acks = Acks.All;
            options.EnableIdempotence = true;
        });
    });
```

The documentation for the `Confluent.Kafka` `ConsumerConfig` and `ProducerConfig` can be consulted for specific options. If there are any options that are not exposed via the `KafkaOptions` class, they can be set by providing the relevant configuration object:

```c#
services.AddHopper()
    .UseKafka(builder =>
    {
        builder.Configure("local", options =>
        {
            options.BootstrapServers = "localhost:9092";
            // ... other properties
            options.ConsumerConfig = new ConsumerConfig
            {
                // ... Confluent.Kafka specific options
            };
            options.ProducerConfig = new ProducerConfig
            {
                // ... Confluent.Kafka specific options
            };
        });
    });
```

Alternatively, you can use the `ConsumerBuilder` and `ProducerBuilder` properties to customize the Kafka clients:

```c#
services.AddHopper()
    .UseKafka(builder =>
    {
        builder.Configure("local", options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.ConsumerBuilder = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
            {
                // ... custom consumer configuration
            });
            options.ProducerBuilder = new ProducerBuilder<Null, string>(new ProducerConfig
            {
                // ... custom producer configuration
            });
        });
    });
```

The default JSON settings structure is as follows:

```json
{
  "Shuttle": {
    "Kafka": {
      "local": {
        "BootstrapServers": "localhost:9092",
        "ReplicationFactor": 1,
        "NumPartitions": 1,
        "MessageSendMaxRetries": 3,
        "RetryBackoff": "00:00:01",
        "EnableAutoCommit": false,
        "EnableAutoOffsetStore": false,
        "FlushEnqueue": false,
        "UseCancellationToken": true,
        "ConsumeTimeout": "00:00:30",
        "OperationTimeout": "00:00:30",
        "ConnectionsMaxIdle": "00:00:00",
        "Acks": "All",
        "EnableIdempotence": true
      }
    }
  }
}
```

## Options

| Option | Default | Description |
| --- | --- | --- | 
| `BootstrapServers` | *(required)* | Initial list of brokers as a CSV list of broker host or host:port. |
| `ReplicationFactor` | 1 | The replication factor for the new topic. |
| `NumPartitions` | 1 | The number of partitions for the new topic. |
| `MessageSendMaxRetries` | 3 | How many times to retry sending a failing Message. |
| `RetryBackoff` | "00:00:01" | The backoff time before retrying a protocol request. |
| `EnableAutoCommit` | false | Automatically and periodically commit offsets in the background. |
| `EnableAutoOffsetStore` | false | Automatically store offset of last message provided to application. |
| `FlushEnqueue` | false | If `true` will call `Flush` on the producer after a message has been enqueued. |
| `UseCancellationToken` | true | Indicates whether a cancellation token is used for relevant methods. |
| `ConsumeTimeout` | "00:00:30" | The duration to poll for messages before returning `null`, when the cancellation token is not used. Minimum value: 25ms. |
| `OperationTimeout` | "00:00:30" | The duration to wait for relevant `async` methods to complete before timing out. Minimum value: 25ms. |
| `ConnectionsMaxIdle` | "00:00:00" | Close broker connections after the specified time of inactivity. Minimum value: 0ms. |
| `Acks` | "All" | This field indicates the number of acknowledgements the leader broker must receive from ISR brokers before responding to the request. |
| `EnableIdempotence` | true | When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. |
| `ConsumerBuilder` | null | Custom `ConsumerBuilder<Ignore, string>` instance. When set, `ConsumerConfig` is ignored. |
| `ProducerBuilder` | null | Custom `ProducerBuilder<Null, string>` instance. When set, `ProducerConfig` is ignored. |
| `ConsumerConfig` | null | Custom `Confluent.Kafka.ConsumerConfig` instance. |
| `ProducerConfig` | null | Custom `Confluent.Kafka.ProducerConfig` instance. |
