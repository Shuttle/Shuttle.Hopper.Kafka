using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;
using System.Net;
using Exception = System.Exception;

namespace Shuttle.Hopper.Kafka;

public class KafkaStream : ITransport, ICreateTransport, IDeleteTransport, IPurgeTransport, IDisposable
{
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaOptions _kafkaOptions;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly TimeSpan _operationTimeout;
    private readonly IProducer<Null, string> _producer;
    private readonly Queue<ReceivedMessage> _receivedMessages = new();

    private bool _disposed;
    private bool _subscribed;

    public KafkaStream(ServiceBusOptions serviceBusOptions, KafkaOptions kafkaOptions, TransportUri uri)
    {
        _serviceBusOptions = serviceBusOptions;
        _kafkaOptions = Guard.AgainstNull(kafkaOptions);

        Uri = Guard.AgainstNull(uri);
        
        Topic = Uri.TransportName;

        _operationTimeout = _kafkaOptions.OperationTimeout;

        _consumerConfig = _kafkaOptions.ConsumerConfig ?? new()
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = Topic,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _kafkaOptions.EnableAutoCommit,
            EnableAutoOffsetStore = _kafkaOptions.EnableAutoOffsetStore,
            ConnectionsMaxIdleMs = (int)_kafkaOptions.ConnectionsMaxIdle.TotalMilliseconds
        };

        var consumerBuilder = _kafkaOptions.ConsumerBuilder ?? new ConsumerBuilder<Ignore, string>(_consumerConfig);
        
        _consumer = consumerBuilder.Build();

        var producerConfig = _kafkaOptions.ProducerConfig ?? new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            ClientId = Dns.GetHostName(),
            Acks = _kafkaOptions.Acks,
            MessageSendMaxRetries = _kafkaOptions.MessageSendMaxRetries,
            RetryBackoffMs = (int)_kafkaOptions.RetryBackoff.TotalMilliseconds,
            EnableIdempotence = _kafkaOptions.EnableIdempotence,
            ConnectionsMaxIdleMs = (int)_kafkaOptions.ConnectionsMaxIdle.TotalMilliseconds
        };

        var producerBuilder = _kafkaOptions.ProducerBuilder ?? new ProducerBuilder<Null, string>(producerConfig);

        _producer = producerBuilder.Build();
    }

    public string Topic { get; }

    public TransportUri Uri { get; }

    public void Dispose()
    {
        _lock.Wait(CancellationToken.None);

        try
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.producer.flush/starting]")).GetAwaiter().GetResult();
                _producer.Flush(_operationTimeout);
                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.producer.flush/completed]")).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // ignore
                 _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.producer.flush/exception]", ex)).GetAwaiter().GetResult();
            }

            _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.producer.dispose/starting]")).GetAwaiter().GetResult();
            _producer.Dispose();
            _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.producer.dispose/completed]")).GetAwaiter().GetResult();

            try
            {
                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.unsubscribe/starting]")).GetAwaiter().GetResult();
                _consumer.Unsubscribe();
                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.unsubscribe/completed]")).GetAwaiter().GetResult();

                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.close/starting]")).GetAwaiter().GetResult();
                _consumer.Close();
                _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.close/completed]")).GetAwaiter().GetResult();
            }
            catch
            {
                // ignore
            }

            _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.dispose/starting]")).GetAwaiter().GetResult();
            _consumer.Dispose();
            _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[dispose.consumer.dispose/starting]")).GetAwaiter().GetResult();

            _disposed = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AcknowledgeAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        if (Guard.AgainstNull(acknowledgementToken) is not AcknowledgementToken token)
        {
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return;
            }

            if (!(_consumerConfig.EnableAutoCommit ?? false) &&
                !(_consumerConfig.EnableAutoOffsetStore ?? false))
            {
                if (!(_consumerConfig.EnableAutoCommit ?? false))
                {
                    _consumer.Commit(token.ConsumeResult);
                }

                if (!(_consumerConfig.EnableAutoOffsetStore ?? false))
                {
                    _consumer.StoreOffset(token.ConsumeResult);
                }
            }

            await _serviceBusOptions.MessageAcknowledged.InvokeAsync(new(this, acknowledgementToken), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create/starting]"), cancellationToken);

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            using var client = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _consumerConfig.BootstrapServers
            }).Build();

            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create.get-metadata/starting]"), cancellationToken);

            var metadata = client.GetMetadata(Topic, _operationTimeout);

            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create.get-metadata/completed]"), cancellationToken);

            if (metadata == null)
            {
                await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create.create-topics/starting]"), cancellationToken);

                await client.CreateTopicsAsync([
                    new()
                    {
                        Name = Topic,
                        ReplicationFactor = _kafkaOptions.ReplicationFactor,
                        NumPartitions = _kafkaOptions.NumPartitions
                    }
                ]).ConfigureAwait(false);

                await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create.create-topics/completed]"), cancellationToken);
            }

            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create/completed]"), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[create/cancelled]"), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[delete/starting]"), cancellationToken);

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            using (var client = new AdminClientBuilder(new AdminClientConfig
                   {
                       BootstrapServers = _kafkaOptions.BootstrapServers
                   }).Build())
            {
                var metadata = client.GetMetadata(Topic, _operationTimeout);

                if (metadata == null)
                {
                    return;
                }

                try
                {
                    await client.DeleteTopicsAsync(new List<string>
                    {
                        Topic
                    }, new() { OperationTimeout = _operationTimeout }).ConfigureAwait(false);
                }
                catch (DeleteTopicsException)
                {
                }
                catch (AggregateException ex) when (ex.InnerException is DeleteTopicsException)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[delete/cancelled]"), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[delete/completed]"), cancellationToken);
    }

    public async Task SendAsync(TransportMessage transportMessage, Stream stream, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(transportMessage, nameof(transportMessage));
        Guard.AgainstNull(stream, nameof(stream));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return;
            }

            var value = Convert.ToBase64String(await stream.ToBytesAsync().ConfigureAwait(false));

            // Intentionally use Produce (fire-and-forget) for higher throughput.  Delivery is awaited later via Flush, not per message.
            _producer.Produce(Topic,
                new()
                {
                    Value = value
                });

            if (!_kafkaOptions.FlushEnqueue)
            {
                return;
            }

            if (_kafkaOptions.UseCancellationToken)
            {
                try
                {
                    _producer.Flush(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
            else
            {
                _producer.Flush(_operationTimeout);
            }

            await _serviceBusOptions.MessageSent.InvokeAsync(new(this, transportMessage, stream), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _lock.Release();
        }
    }

    public TransportType Type => TransportType.Stream;

    public async Task<ReceivedMessage?> ReceiveAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return null;
            }

            if (_receivedMessages.Count > 0)
            {
                return _receivedMessages.Dequeue();
            }

            ReadMessage(cancellationToken);

            var receivedMessage = _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;

            if (receivedMessage != null)
            {
                await _serviceBusOptions.MessageReceived.InvokeAsync(new(this, receivedMessage), cancellationToken);
            }

            return receivedMessage;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask<bool> HasPendingAsync(CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[has-pending/starting]"), cancellationToken);

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_receivedMessages.Count > 0 || _disposed)
            {
                return false;
            }

            ReadMessage(cancellationToken);

            var result = _receivedMessages.Count > 0;

            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[has-pending]", result), cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[has-pending/cancelled]", false), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        return false;
    }

    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[purge/starting]"), cancellationToken);

        await DeleteAsync(cancellationToken);
        await CreateAsync(cancellationToken);

        await _serviceBusOptions.TransportOperation.InvokeAsync(new(this, "[purge/completed]"), cancellationToken);
    }

    private void ReadMessage(CancellationToken cancellationToken)
    {
        if (!_subscribed)
        {
            try
            {
                _consumer.Subscribe(Topic);
                _subscribed = true;
            }
            catch (Exception)
            {
                return;
            }
        }

        ConsumeResult<Ignore, string>? consumeResult = null;

        try
        {
            consumeResult = _kafkaOptions.UseCancellationToken ? _consumer.Consume(cancellationToken) : _consumer.Consume(_kafkaOptions.ConsumeTimeout);
        }
        catch (OperationCanceledException)
        {
        }

        if (consumeResult == null)
        {
            return;
        }

        var acknowledgementToken = new AcknowledgementToken(Guid.NewGuid(), consumeResult);

        _receivedMessages.Enqueue(new(new MemoryStream(Convert.FromBase64String(consumeResult.Message.Value)), acknowledgementToken));
    }

    public async Task ReleaseAsync(object acknowledgementToken, CancellationToken cancellationToken = default)
    {
        if (Guard.AgainstNull(acknowledgementToken) is not AcknowledgementToken token)
        {
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_disposed)
            {
                return;
            }

            _receivedMessages.Enqueue(new(new MemoryStream(Convert.FromBase64String(token.ConsumeResult.Message.Value)), acknowledgementToken));

            await _serviceBusOptions.MessageReleased.InvokeAsync(new(this, acknowledgementToken), cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    internal class AcknowledgementToken(Guid messageId, ConsumeResult<Ignore, string> consumeResult)
    {
        public ConsumeResult<Ignore, string> ConsumeResult { get; } = consumeResult;

        public Guid MessageId { get; } = messageId;
    }
}