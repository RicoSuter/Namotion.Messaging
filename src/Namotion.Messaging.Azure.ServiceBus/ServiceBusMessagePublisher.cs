using System;
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

        /// <inheritdoc/>
        public async Task SendAsync(IEnumerable<Abstractions.Message> messages, CancellationToken cancellationToken = default)
        {
            await _queueClient.SendAsync(messages.Select(m => CreateMessage(m)).ToList());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _queueClient.CloseAsync().GetAwaiter().GetResult();
        }

        private Microsoft.Azure.ServiceBus.Message CreateMessage(Abstractions.Message message)
        {
            var abstractMessage = new Microsoft.Azure.ServiceBus.Message(message.Content)
            {
                MessageId = message.Id ?? Guid.NewGuid().ToString()
            };

            foreach (var property in message.Properties)
            {
                abstractMessage.UserProperties[property.Key] = property.Value;
            }

            return abstractMessage;
        }
    }
}