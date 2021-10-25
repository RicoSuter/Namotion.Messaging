using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Namotion.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// A Service Bus message publisher.
    /// </summary>
    public class ServiceBusMessagePublisher : IMessagePublisher
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly bool _disposeClient;

        private ServiceBusMessagePublisher(ServiceBusClient serviceBusClient, string queueName, bool disposeClient = false)
        {
            _client = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
            _sender = _client.CreateSender(queueName);
            _disposeClient = disposeClient;
        }

        /// <summary>
        /// Creates a new Service Bus publisher from a client.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        /// <param name="queueName">The queue or topic name.</param>
        /// <param name="disposeClient">Specifies whether to dispose the client when this receiver is disposed.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher CreateFromServiceBusClient(ServiceBusClient serviceBusClient, string queueName, bool disposeClient = false)
        {
            return new ServiceBusMessagePublisher(serviceBusClient, queueName, disposeClient);
        }

        /// <summary>
        /// Creates a new Service Bus publisher from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="queueName">The queue or topic name.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(string connectionString, string queueName)
        {
            var client = new ServiceBusClient(connectionString);
            return new ServiceBusMessagePublisher(client, queueName, true);
        }

        /// <inheritdoc/>
        public async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            await _sender.SendMessagesAsync(messages.Select(m => CreateMessage(m)).ToList(), cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_sender != null)
            {
                await _sender.DisposeAsync();
            }

            if (_disposeClient && _client != null)
            {
                await _client.DisposeAsync();
            }
        }

        private ServiceBusMessage CreateMessage(Message abstractMessage)
        {
            var message = new ServiceBusMessage(abstractMessage.Content)
            {
                MessageId = abstractMessage.Id ?? Guid.NewGuid().ToString(),
                SessionId = abstractMessage.PartitionId
            };

            if (abstractMessage.EnqueueTime.HasValue)
            {
                message.ScheduledEnqueueTime = abstractMessage.EnqueueTime.Value.ToUniversalTime().DateTime;
            }

            foreach (var property in abstractMessage.Properties)
            {
                message.ApplicationProperties[property.Key] = property.Value;
            }

            return message;
        }
    }
}