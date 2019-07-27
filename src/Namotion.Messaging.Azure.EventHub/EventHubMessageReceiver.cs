using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Namotion.Messaging.Azure.EventHub
{
    public class EventHubMessageReceiver : IMessageReceiver
    {
        private readonly EventProcessorHost _host;
        private readonly EventProcessorOptions _processorOptions;
        private readonly ILogger _logger;

        public EventHubMessageReceiver(
            string eventHubPath,
            string consumerGroupName,
            string eventHubConnectionString,
            string storageConnectionString,
            string leaseContainerName,
            ILogger logger = null)
            : this(new EventProcessorHost(eventHubPath, consumerGroupName, eventHubConnectionString, storageConnectionString, leaseContainerName),
                  EventProcessorOptions.DefaultOptions, logger)
        {
        }

        public EventHubMessageReceiver(EventProcessorHost eventProcessorHost, ILogger logger = null)
            : this(eventProcessorHost, EventProcessorOptions.DefaultOptions, logger)
        {
        }

        public EventHubMessageReceiver(EventProcessorHost eventProcessorHost, EventProcessorOptions processorOptions, ILogger logger = null)
        {
            _host = eventProcessorHost;
            _processorOptions = processorOptions;
            _logger = logger ?? NullLogger.Instance;
        }

        public async Task ListenAsync(Func<IEnumerable<QueueMessage>, CancellationToken, Task> onMessageAsync, CancellationToken cancellationToken = default)
        {
            try
            {
                await _host.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(onMessageAsync, _host, _logger, cancellationToken), _processorOptions);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                await _host.UnregisterEventProcessorAsync();
            }
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ConfirmAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            // There is no message confirmation in Event Hubs, only checkpointing after processing
            return Task.CompletedTask;
        }

        public Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            // There is no message rejection in Event Hubs
            _logger.LogWarning("Message has been rejected which is not supported by Event Hub.");
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            // There is no dead letter queue in Event Hubs
            _logger.LogWarning("Message has been dead lettered which is not supported by Event Hub.");
            return Task.CompletedTask;
        }

        public Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            // Keep alive is not needed as there is no timeout in Event Hubs
            return Task.CompletedTask;
        }

        internal class EventProcessorFactory : IEventProcessorFactory
        {
            private readonly Func<IEnumerable<QueueMessage>, CancellationToken, Task> _onMessageAsync;
            private readonly EventProcessorHost _host;
            private readonly ILogger _logger;
            private readonly CancellationToken _cancellationToken;

            public EventProcessorFactory(
                Func<IEnumerable<QueueMessage>, CancellationToken, Task> onMessageAsync,
                EventProcessorHost host,
                ILogger logger,
                CancellationToken cancellationToken)
            {
                _onMessageAsync = onMessageAsync;
                _host = host;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public IEventProcessor CreateEventProcessor(PartitionContext context)
            {
                return new EventProcessor(_onMessageAsync, _host, _logger, _cancellationToken);
            }
        }

        internal class EventProcessor : IEventProcessor
        {
            private const string SequenceNumberProperty = "x-opt-sequence-number";
            private const string OffsetProperty = "x-opt-offset";

            private readonly Func<IEnumerable<QueueMessage>, CancellationToken, Task> _onMessageAsync;
            private readonly EventProcessorHost _host;
            private readonly ILogger _logger;
            private readonly CancellationToken _cancellationToken;
            private IDisposable _scope;

            public EventProcessor(
                Func<IEnumerable<QueueMessage>, CancellationToken, Task> onMessageAsync,
                EventProcessorHost host,
                ILogger logger,
                CancellationToken cancellationToken)
            {
                _onMessageAsync = onMessageAsync;
                _host = host;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public Task OpenAsync(PartitionContext context)
            {
                _logger.LogTrace("Start listening on partition {PartitionId}.", context.PartitionId);
                _scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    { "EventHub.HostName", _host.HostName },
                    { "EventHub.Path", context.EventHubPath },
                    { "EventHub.ConsumerGroupName", context.ConsumerGroupName },
                    { "EventHub.PartitionId", context.PartitionId },
                });

                return Task.CompletedTask;
            }

            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    { "EventHub.Batch.Scope", Guid.NewGuid() },
                    { "EventHub.Batch.MessageCount", messages.Count() },

                    { "EventHub.Batch.StartSequenceNumber", messages.First().SystemProperties[SequenceNumberProperty] },
                    { "EventHub.Batch.EndSequenceNumber", messages.Last().SystemProperties[SequenceNumberProperty] },

                    { "EventHub.Batch.StartOffset", messages.First().SystemProperties[OffsetProperty] },
                    { "EventHub.Batch.EndOffset", messages.Last().SystemProperties[OffsetProperty] },
                }))
                {
                    await _onMessageAsync(messages.Select(m => new QueueMessage(m.Body.Array)
                    {
                        PartitionId = context.PartitionId,
                        Properties = m.Properties.ToDictionary(p => p.Key, p => p.Value),
                        SystemProperties = m.SystemProperties.ToDictionary(p => p.Key, p => p.Value)
                    }), _cancellationToken);

                    _cancellationToken.ThrowIfCancellationRequested();
                    await context.CheckpointAsync();

                    _logger.LogTrace("Processed {Count} messages in partition {PartitionId}.", messages.Count(), context.PartitionId);
                }
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                _logger.LogError(error, "Error in partition {PartitionId} processor.", context.PartitionId);
                return Task.CompletedTask;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                _logger.LogTrace("Stop listening on partition {PartitionId}.", context.PartitionId);
                _scope?.Dispose();
                return Task.CompletedTask;
            }
        }
    }
}
