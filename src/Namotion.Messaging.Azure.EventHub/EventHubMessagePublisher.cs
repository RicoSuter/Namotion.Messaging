using Microsoft.Azure.EventHubs;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Azure.EventHub
{
    public class EventHubMessagePublisher : IMessagePublisher
    {
        private readonly EventHubClient _client;
        private readonly long _maxMessageSize;

        public EventHubMessagePublisher(string connectionString, long maxMessageSize = 262144)
            : this(EventHubClient.CreateFromConnectionString(connectionString))
        {
            _maxMessageSize = maxMessageSize;
        }

        public EventHubMessagePublisher(EventHubClient client)
        {
            _client = client;
        }

        public async Task SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(messages
                .GroupBy(m => m.PartitionId)
                .Select(g => Task.Run(async () =>
                {
                    var batch = _client.CreateBatch(new BatchOptions
                    {
                        PartitionKey = g.Key,
                        MaxMessageSize = _maxMessageSize
                    });

                    try
                    {
                        foreach (var message in g)
                        {
                            if (!batch.TryAdd(CreateEventData(message)))
                            {
                                await _client.SendAsync(batch);

                                batch.Dispose();
                                batch = _client.CreateBatch(new BatchOptions
                                {
                                    PartitionKey = g.Key,
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

        private static EventData CreateEventData(Message message)
        {
            var eventData = new EventData(message.Content);

            foreach (var property in message.Properties)
            {
                eventData.Properties[property.Key] = property.Value;
            }

            return eventData;
        }

        public void Dispose()
        {
            _client.Close();
        }
    }
}
