using Microsoft.Azure.EventHubs;
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
        private readonly EventHubClient _client;
        private readonly long _maxMessageSize;

        private EventHubMessagePublisher(EventHubClient client, long maxMessageSize)
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
        public static IMessagePublisher CreateFromEventHubClient(EventHubClient client, long maxMessageSize = 262144)
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
            return new EventHubMessagePublisher(EventHubClient.CreateFromConnectionString(connectionString), maxMessageSize);
        }

        /// <inheritdoc/>
        public async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            await Task.WhenAll(messages
                .GroupBy(m => m.PartitionId)
                .Select(messageGroup => Task.Run(async () =>
                {
                    var batch = _client.CreateBatch(new BatchOptions
                    {
                        PartitionKey = messageGroup.Key,
                        MaxMessageSize = _maxMessageSize
                    });

                    try
                    {
                        foreach (var message in messageGroup)
                        {
                            if (!batch.TryAdd(CreateEventData(message)))
                            {
                                await _client.SendAsync(batch);

                                batch.Dispose();
                                batch = _client.CreateBatch(new BatchOptions
                                {
                                    PartitionKey = messageGroup.Key,
                                    MaxMessageSize = _maxMessageSize
                                });
                            }
                        }

                        if (batch.Count > 0)
                        {
                            await _client.SendAsync(batch);
                        }
                    }
                    finally
                    {
                        batch.Dispose();
                    }
                }))).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.Close();
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
    }
}
