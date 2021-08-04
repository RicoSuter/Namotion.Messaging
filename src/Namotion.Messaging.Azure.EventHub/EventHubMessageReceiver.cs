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

namespace Namotion.Messaging.Azure.EventHub
{
    /// <summary>
    /// An Event Hub message receiver.
    /// </summary>
    public class EventHubMessageReceiver : IMessageReceiver
    {
        private readonly EventProcessorClient _client;
        private readonly ILogger _logger;

        private EventHubMessageReceiver(EventProcessorClient eventProcessorClient, ILogger logger = null)
        {
            _client = eventProcessorClient;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="eventProcessorClient">The event processor client.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver CreateFromEventProcessorHost(
            EventProcessorClient eventProcessorClient,
            ILogger logger = null)
        {
            return new EventHubMessageReceiver(eventProcessorClient, logger);
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="BlobContainerClient"/>.
        /// </summary>
        /// <param name="eventHubPath">The event hub path.</param>
        /// <param name="consumerGroupName">The consumer group name.</param>
        /// <param name="eventHubConnectionString">The event hub connection string.</param>
        /// <param name="storageConnectionString">The stoarge connection string.</param>
        /// <param name="leaseContainerName">The lease container name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver Create(
            string eventHubPath,
            string consumerGroupName,
            string eventHubConnectionString,
            string storageConnectionString,
            string leaseContainerName,
            ILogger logger = null)
        {
            return new EventHubMessageReceiver(
                new EventProcessorClient(new BlobContainerClient(storageConnectionString, leaseContainerName), consumerGroupName, eventHubConnectionString),
                logger);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            var processor = new EventProcessor(handleMessages, _logger, cancellationToken);
            try
            {
                _client.ProcessEventAsync += processor.ProcessEventsAsync;
                _client.ProcessErrorAsync += processor.ProcessErrorAsync;

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
                try
                {
                    await _client.StopProcessingAsync(cancellationToken);
                }
                finally
                {
                    _client.ProcessEventAsync -= processor.ProcessEventsAsync;
                    _client.ProcessErrorAsync -= processor.ProcessErrorAsync;
                }
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
            _logger.LogWarning("Message rejection is not supported by Event Hub.");
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

        internal class EventProcessor
        {
            private const string SequenceNumberProperty = "x-opt-sequence-number";
            private const string OffsetProperty = "x-opt-offset";

            private readonly Func<IReadOnlyCollection<Message>, CancellationToken, Task> _handleMessages;
            private readonly ILogger _logger;
            private readonly CancellationToken _cancellationToken;

            private IDisposable _scope;

            public EventProcessor(
                Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages,
                ILogger logger,
                CancellationToken cancellationToken)
            {
                _handleMessages = handleMessages;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public async Task ProcessEventsAsync(ProcessEventArgs args)
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    { "EventHub.Batch.Scope", Guid.NewGuid() },
                    //{ "EventHub.Batch.MessageCount", messages.Count() },
                    //{ "EventHub.Batch.StartSequenceNumber", messages.First().SystemProperties[SequenceNumberProperty] },
                    //{ "EventHub.Batch.EndSequenceNumber", messages.Last().SystemProperties[SequenceNumberProperty] },
                    //{ "EventHub.Batch.StartOffset", messages.First().SystemProperties[OffsetProperty] },
                    //{ "EventHub.Batch.EndOffset", messages.Last().SystemProperties[OffsetProperty] },
                }))
                {
                    if (args.HasEvent)
                    {
                        try
                        {
                            var messages = new[] { args.Data };
                            await _handleMessages(messages.Select(m => new Message(
                                id: m.PartitionKey + "-" + m.SequenceNumber,
                                content: m.EventBody.ToArray(),
                                partitionId: args.Partition.PartitionId,
                                properties: m.Properties.ToDictionary(p => p.Key, p => p.Value),
                                systemProperties: m.SystemProperties.ToDictionary(p => p.Key, p => p.Value))
                            ).ToArray(), _cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "An unexpected error occurred in the message handler.");
                        }

                        _cancellationToken.ThrowIfCancellationRequested();
                        await args.UpdateCheckpointAsync(_cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public Task ProcessErrorAsync(ProcessErrorEventArgs args)
            {
                _logger.LogWarning(args.Exception, "Unable to process events " +
                    "for consumer group {ConsumerGroupName} and path {EventHubPath} and partition {PartitionId} in operation {Operation}.",
                    "n/a", "n/a", args.PartitionId, args.Operation);

                return Task.CompletedTask;
            }
        }
    }
}
