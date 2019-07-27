using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Azure.ServiceBus
{
    public class ServiceBusMessagePublisher : IMessagePublisher
    {
        private readonly QueueClient _queueClient;

        public ServiceBusMessagePublisher(string connectionString, string entityPath)
        {
            _queueClient = new QueueClient(connectionString, entityPath);
        }

        public async Task SendAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            await _queueClient.SendAsync(messages.Select(m => CreateMessage(m)).ToList());
        }

        public void Dispose()
        {
            _queueClient.CloseAsync().GetAwaiter().GetResult();
        }

        private Message CreateMessage(QueueMessage message)
        {
            var m = new Message(message.Content)
            {
                MessageId = message.Id
            };

            foreach (var property in message.Properties)
            {
                m.UserProperties[property.Key] = property.Value;
            }

            return m;
        }
    }
}