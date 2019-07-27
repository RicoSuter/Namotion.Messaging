using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Namotion.Messaging.Azure.EventHub
{
    public class EventHubMessagePublisher : IMessagePublisher
    {
        private readonly EventHubClient _client;
        private readonly ILogger _logger;

        public EventHubMessagePublisher(string connectionString, ILogger logger = null)
            : this(EventHubClient.CreateFromConnectionString(connectionString), logger)
        {
        }

        public EventHubMessagePublisher(EventHubClient client, ILogger logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger.Instance;
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
                        _logger.LogTrace("Published {Count} messages to any partition on event hub {EventHubName}.", g.Count(), _client.EventHubName);
                    }
                    else
                    {
                        await _client.SendAsync(g.Select(m => CreateEventData(m)), g.Key).ConfigureAwait(false);
                        _logger.LogTrace("Published {Count} messages to partition {PartitionId} on event hub {EventHubName}.", g.Count(), g.Key, _client.EventHubName);
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
