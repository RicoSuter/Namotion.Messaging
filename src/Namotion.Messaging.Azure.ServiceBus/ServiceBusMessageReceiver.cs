using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Azure.ServiceBus
{
    public class ServiceBusMessageReceiver : Abstractions.IMessageReceiver
    {
        private const string LockTokenProperty = "LockToken";

        private MessageReceiver _messageReceiver;

        public ServiceBusMessageReceiver(string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock)
            : this(new MessageReceiver(connectionString, entityPath, receiveMode))
        {
        }

        public ServiceBusMessageReceiver(MessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(MessageReceiver));
        }

        public async Task ListenAsync(Func<IEnumerable<QueueMessage>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    await handleMessages(new QueueMessage[] { ToMessage(message) }, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await _messageReceiver.CloseAsync().ConfigureAwait(false);
            }
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.RenewLockAsync((string)message.SystemProperties[LockTokenProperty]).ConfigureAwait(false);
        }

        public async Task ConfirmAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(messages.Select(m =>
            {
                return _messageReceiver.CompleteAsync((string)m.SystemProperties[LockTokenProperty]);
            })).ConfigureAwait(false);
        }

        public async Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.AbandonAsync((string)message.SystemProperties[LockTokenProperty]).ConfigureAwait(false);
        }

        public async Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.DeadLetterAsync((string)message.SystemProperties[LockTokenProperty], reason, errorDescription).ConfigureAwait(false);
        }

        private QueueMessage ToMessage(Message message)
        {
            var m = new QueueMessage(message.Body)
            {
                Id = message.MessageId,
                DequeueCount = message.SystemProperties.DeliveryCount,
                SystemProperties =
                {
                    { LockTokenProperty, message.SystemProperties.LockToken }
                }
            };

            foreach (var property in message.UserProperties)
            {
                m.Properties[property.Key] = property.Value;
            }

            return m;
        }
    }
}