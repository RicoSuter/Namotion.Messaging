﻿using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Namotion.Messaging.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Azure.EventHub
{
    /// <summary>
    /// An Event Hub publisher.
    /// </summary>
    public class EventHubMessagePublisher : IMessagePublisher
    {
        private readonly EventHubProducerClient _client;
        private readonly long _maxMessageSize;

        private EventHubMessagePublisher(EventHubProducerClient client, long maxMessageSize)
        {
            _client = client;
            _maxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// Creates a new Event Hub publisher with a client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="maxMessageSize">The maximum message size.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher CreateFromEventHubClient(EventHubProducerClient client, long maxMessageSize = 262144)
        {
            return new EventHubMessagePublisher(client, maxMessageSize);
        }

        /// <summary>
        /// Creates a new Event Hub publisher from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="maxMessageSize">The maximum message size.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(string connectionString, long maxMessageSize = 262144)
        {
            return new EventHubMessagePublisher(new EventHubProducerClient(connectionString), maxMessageSize);
        }

        /// <summary>
        /// Creates a new Event Hub publisher from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="eventHubName">The event hub name.</param>
        /// <param name="maxMessageSize">The maximum message size.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(string connectionString, string eventHubName, long maxMessageSize = 262144)
        {
            return new EventHubMessagePublisher(new EventHubProducerClient(connectionString, eventHubName), maxMessageSize);
        }

        /// <inheritdoc/>
        public async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            if (messages.Any(m => m.EnqueueTime.HasValue))
            {
                throw new ArgumentException("The EnqueueTime property is not supported with Event Hubs.");
            }

            try
            {
                await Task.WhenAll(messages
                    .GroupBy(m => m.PartitionId)
                    .Select(messageGroup => Task.Run(async () =>
                    {
                        var batchOptions = new CreateBatchOptions
                        {
                            PartitionKey = messageGroup.Key,
                            MaximumSizeInBytes = _maxMessageSize
                        };

                        var batch = await _client.CreateBatchAsync(batchOptions, cancellationToken);
                        try
                        {
                            foreach (var message in messageGroup)
                            {
                                if (!batch.TryAdd(CreateEventData(message)))
                                {
                                    await _client.SendAsync(batch, cancellationToken).ConfigureAwait(false);

                                    batch.Dispose();
                                    batch = await _client.CreateBatchAsync(new CreateBatchOptions
                                    {
                                        PartitionKey = messageGroup.Key,
                                        MaximumSizeInBytes = _maxMessageSize
                                    });
                                }
                            }

                            if (batch.Count > 0)
                            {
                                await _client.SendAsync(batch, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            batch.Dispose();
                        }
                    }))).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { throw; }
            catch (Exception e)
            {
                throw new MessagePublishingFailedException(null, "Could not publish some of the messages.", e);
            }
        }

        private static EventData CreateEventData(Message message)
        {
            var eventData = new EventData(message.Content);

            foreach (var property in message.Properties)
            {
                eventData.Properties[property.Key] = property.Value;
            }

            return eventData;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return _client.DisposeAsync();
        }
    }
}
