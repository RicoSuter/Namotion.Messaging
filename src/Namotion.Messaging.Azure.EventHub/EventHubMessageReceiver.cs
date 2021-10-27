using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Namotion.Messaging.Exceptions;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Primitives;

namespace Namotion.Messaging.Azure.EventHub
{
    /// <summary>
    /// An Event Hub message receiver.
    /// </summary>
    public class EventHubMessageReceiver : IMessageReceiver
    {
        private readonly InternalEventProcessorClient _client;
        private readonly ILogger _logger;

        private EventHubMessageReceiver(InternalEventProcessorClient eventProcessorClient, ILogger logger = null)
        {
            _client = eventProcessorClient;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="consumerGroupName">The consumer group name.</param>
        /// <param name="eventHubConnectionString">The event hub connection string.</param>
        /// <param name="eventHubName">The event hub name.</param>
        /// <param name="storageConnectionString">The stoarge connection string.</param>
        /// <param name="leaseContainerName">The lease container name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver Create(
            string consumerGroupName,
            string eventHubConnectionString,
            string eventHubName,
            string storageConnectionString,
            string leaseContainerName,
            ILogger logger = null)
        {
            return new EventHubMessageReceiver(
                new InternalEventProcessorClient(
                    new BlobContainerClient(storageConnectionString, leaseContainerName), 
                    consumerGroupName, eventHubConnectionString, eventHubName, 
                    new EventProcessorClientOptions(), logger),
                logger);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            try
            {
                _client.HandleMessages = handleMessages;
             
                await _client.StartProcessingAsync(cancellationToken);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException) { throw; }
            catch (Exception e)
            {
                throw new MessageReceivingFailedException("Registration of the message listener failed.", e);
            }
            finally
            {
                await _client.StopProcessingAsync(); // no cancellation token to ensure it's stopped
            }
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            // There is no message confirmation in Event Hubs, only checkpointing after processing
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            // There is no message rejection in Event Hubs
            _logger?.LogWarning("Message rejection is not supported by Event Hub.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            // Keep alive is not needed as there is no timeout in Event Hubs
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
#pragma warning disable CS1998
        public async ValueTask DisposeAsync()
        {
        }

        internal class InternalEventProcessorClient : EventProcessorClient
        {
            private const string SequenceNumberProperty = "x-opt-sequence-number";
            private const string OffsetProperty = "x-opt-offset";

            public Func<IReadOnlyCollection<Message>, CancellationToken, Task> HandleMessages { get; set; }

            private readonly Dictionary<string, ProcessEventArgs> _lastProcessEventArgs = new Dictionary<string, ProcessEventArgs>();
            private readonly ILogger _logger;

            public InternalEventProcessorClient(
                BlobContainerClient checkpointStore, 
                string consumerGroup, 
                string connectionString, 
                string eventHubName, 
                EventProcessorClientOptions clientOptions,
                ILogger logger)
                : base(checkpointStore, consumerGroup, connectionString, eventHubName, clientOptions)
            {
                _logger = logger;

                ProcessEventAsync += OnProcessEventAsync;
                ProcessErrorAsync += OnProcessErrorAsync;
            }

            private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
            {
                _logger?.LogWarning(args.Exception, "Unable to process events " +
                    "for consumer group {ConsumerGroupName} and path {EventHubPath} and partition {PartitionId} in operation {Operation}.",
                    "n/a", "n/a", args.PartitionId, args.Operation);

                return Task.CompletedTask;
            }

            private Task OnProcessEventAsync(ProcessEventArgs args)
            {
                _lastProcessEventArgs[args.Partition.PartitionId] = args;
                return Task.CompletedTask;
            }

            protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events, EventProcessorPartition partition, CancellationToken cancellationToken)
            {
                var messages = events.ToArray();
                using (_logger?.BeginScope(new Dictionary<string, object>
                {
                    { "EventHub.Batch.Scope", Guid.NewGuid() },
                    { "EventHub.Batch.MessageCount", 1 },
                    { "EventHub.Batch.StartSequenceNumber", messages.First().SystemProperties[SequenceNumberProperty] },
                    { "EventHub.Batch.EndSequenceNumber", messages.Last().SystemProperties[SequenceNumberProperty] },
                    { "EventHub.Batch.StartOffset", messages.First().SystemProperties[OffsetProperty] },
                    { "EventHub.Batch.EndOffset", messages.Last().SystemProperties[OffsetProperty] },
                }))
                {
                    try
                    {
                        await HandleMessages(messages.Select(m => new Message(
                            id: m.PartitionKey + "-" + m.SequenceNumber,
                            content: m.EventBody.ToArray(),
                            partitionId: partition.PartitionId,
                            properties: m.Properties.ToDictionary(p => p.Key, p => p.Value),
                            systemProperties: m.SystemProperties.ToDictionary(p => p.Key, p => p.Value))
                        ).ToArray(), cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger?.LogError(exception, "An unexpected error occurred in the message handler.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    await base.OnProcessingEventBatchAsync(events, partition, cancellationToken).ConfigureAwait(false);

                    await _lastProcessEventArgs[partition.PartitionId]
                        .UpdateCheckpointAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
