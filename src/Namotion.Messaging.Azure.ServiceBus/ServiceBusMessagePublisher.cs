using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// A Service Bus message publisher.
    /// </summary>
    public class ServiceBusMessagePublisher : IMessagePublisher
    {
        private readonly QueueClient _client;

        private ServiceBusMessagePublisher(QueueClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates a new Service Bus publisher from a queue client.
        /// </summary>
        /// <param name="queueClient">The queue client.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher CreateFromQueueClient(QueueClient queueClient)
        {
            return new ServiceBusMessagePublisher(queueClient);
        }

        /// <summary>
        /// Creates a new Service Bus publisher from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="entityPath">The entity path.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(string connectionString, string entityPath)
        {
            return new ServiceBusMessagePublisher(new QueueClient(connectionString, entityPath));
        }

        /// <inheritdoc/>
        public async Task PublishAsync(IEnumerable<Abstractions.Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            await _client.SendAsync(messages.Select(m => CreateMessage(m)).ToList());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.CloseAsync().GetAwaiter().GetResult();
        }

        private Microsoft.Azure.ServiceBus.Message CreateMessage(Abstractions.Message abstractMessage)
        {
            var message = new Microsoft.Azure.ServiceBus.Message(abstractMessage.Content)
            {
                MessageId = abstractMessage.Id ?? Guid.NewGuid().ToString()
            };

            foreach (var property in abstractMessage.Properties)
            {
                message.UserProperties[property.Key] = property.Value;
            }

            return message;
        }
    }
}