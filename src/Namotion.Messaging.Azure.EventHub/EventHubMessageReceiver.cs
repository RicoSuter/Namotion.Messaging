﻿using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Namotion.Messaging.Exceptions;

namespace Namotion.Messaging.Azure.EventHub
{
    /// <summary>
    /// An Event Hub message receiver.
    /// </summary>
    public class EventHubMessageReceiver : IMessageReceiver
    {
        private readonly EventProcessorHost _host;
        private readonly EventProcessorOptions _processorOptions;
        private readonly ILogger _logger;

        private EventHubMessageReceiver(EventProcessorHost eventProcessorHost, EventProcessorOptions processorOptions, ILogger logger = null)
        {
            _host = eventProcessorHost;
            _processorOptions = processorOptions;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="EventProcessorHost"/>.
        /// </summary>
        /// <param name="eventProcessorHost">The event processor host.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver CreateFromEventProcessorHost(
            EventProcessorHost eventProcessorHost,
            ILogger logger = null)
        {
            return new EventHubMessageReceiver(eventProcessorHost, EventProcessorOptions.DefaultOptions, logger);
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="EventProcessorHost"/>.
        /// </summary>
        /// <param name="eventProcessorHost">The event processor host.</param>
        /// <param name="processorOptions">The processor options.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver CreateFromEventProcessorHost(
            EventProcessorHost eventProcessorHost,
            EventProcessorOptions processorOptions,
            ILogger logger = null)
        {
            return new EventHubMessageReceiver(eventProcessorHost, processorOptions, logger);
        }

        /// <summary>
        /// Creates a new Event Hub message receiver from an <see cref="EventProcessorHost"/>.
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
                new EventProcessorHost(eventHubPath, consumerGroupName, eventHubConnectionString, storageConnectionString, leaseContainerName),
                EventProcessorOptions.DefaultOptions,
                logger);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            try
            {
                var factory = new EventProcessorFactory(handleMessages, _host, _logger, cancellationToken);
                await _host.RegisterEventProcessorFactoryAsync(factory, _processorOptions);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException) { throw; }
            catch (Exception e)
            {
                throw new MessageReceivingFailedException("Registration of the message listener failed.", e);
            }
            finally
            {
                await _host.UnregisterEventProcessorAsync();
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
            _logger.LogWarning("Message has been rejected which is not supported by Event Hub.");
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

        internal class EventProcessorFactory : IEventProcessorFactory
        {
            private readonly Func<IReadOnlyCollection<Message>, CancellationToken, Task> _handleMessages;
            private readonly EventProcessorHost _host;
            private readonly ILogger _logger;
            private readonly CancellationToken _cancellationToken;

            public EventProcessorFactory(
                Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages,
                EventProcessorHost host,
                ILogger logger,
                CancellationToken cancellationToken)
            {
                _handleMessages = handleMessages;
                _host = host;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public IEventProcessor CreateEventProcessor(PartitionContext context)
            {
                return new EventProcessor(_handleMessages, _host, _logger, _cancellationToken);
            }
        }

        internal class EventProcessor : IEventProcessor
        {
            private const string SequenceNumberProperty = "x-opt-sequence-number";
            private const string OffsetProperty = "x-opt-offset";

            private readonly Func<IReadOnlyCollection<Message>, CancellationToken, Task> _handleMessages;
            private readonly EventProcessorHost _host;
            private readonly ILogger _logger;
            private readonly CancellationToken _cancellationToken;

            private IDisposable _scope;

            public EventProcessor(
                Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages,
                EventProcessorHost host,
                ILogger logger,
                CancellationToken cancellationToken)
            {
                _handleMessages = handleMessages;
                _host = host;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public Task OpenAsync(PartitionContext context)
            {
                _logger.LogInformation("Started receiving on partition {PartitionId}.", context.PartitionId);

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
                    try
                    {
                        await _handleMessages(messages.Select(m => new Message(
                            id: m.SystemProperties.PartitionKey + "-" + m.SystemProperties.SequenceNumber,
                            content: m.Body.Array,
                            partitionId: context.PartitionId,
                            properties: m.Properties.ToDictionary(p => p.Key, p => p.Value),
                            systemProperties: m.SystemProperties.ToDictionary(p => p.Key, p => p.Value))
                        ).ToArray(), _cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "An unexpected error occurred in the message handler.");
                    }

                    _cancellationToken.ThrowIfCancellationRequested();
                    await context.CheckpointAsync().ConfigureAwait(false);
                }
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                if (error is ReceiverDisconnectedException)
                {
                    _logger.LogInformation(error, "Receiver for partition has stopped receiving " +
                        "messages because another process has taken over the lease " +
                        "for consumer group {ConsumerGroupName} and path {EventHubPath} and partition {PartitionId}.",
                        context.ConsumerGroupName, context.EventHubPath, context.PartitionId);
                }
                else
                {
                    _logger.LogWarning(error, "Unable to process events " +
                        "for consumer group {ConsumerGroupName} and path {EventHubPath} and partition {PartitionId}.",
                        context.ConsumerGroupName, context.EventHubPath, context.PartitionId);
                }

                return Task.CompletedTask;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                _logger.LogInformation("Stopped receiving on partition {PartitionId}.", context.PartitionId);
                _scope?.Dispose();

                return Task.CompletedTask;
            }
        }
    }
}
