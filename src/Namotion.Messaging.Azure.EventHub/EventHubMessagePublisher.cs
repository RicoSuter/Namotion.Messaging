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

        public EventHubMessagePublisher(string connectionString)
            : this(EventHubClient.CreateFromConnectionString(connectionString))
        {
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
                    if (string.IsNullOrEmpty(g.Key))
                    {
                        await _client.SendAsync(g.Select(m => CreateEventData(m))).ConfigureAwait(false);
                    }
                    else
                    {
                        await _client.SendAsync(g.Select(m => CreateEventData(m)), g.Key).ConfigureAwait(false);
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
