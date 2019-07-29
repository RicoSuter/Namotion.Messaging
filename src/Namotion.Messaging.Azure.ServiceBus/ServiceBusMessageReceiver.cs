using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Namotion.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// A Service Bus message receiver.
    /// </summary>
    public class ServiceBusMessageReceiver : Abstractions.IMessageReceiver
    {
        private const string LockTokenProperty = "LockToken";
        private const string DeliveryCountProperty = "DeliveryCount";

        private MessageReceiver _messageReceiver;

        private ServiceBusMessageReceiver(MessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(MessageReceiver));
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a message receiver.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The message publisher.</returns>
        public static Abstractions.IMessageReceiver CreateFromMessageReceiver(MessageReceiver messageReceiver)
        {
            return new ServiceBusMessageReceiver(messageReceiver);
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="entityPath">The entity path.</param>
        /// <param name="receiveMode">The receive mode (default: PeekLock).</param>
        /// <returns>The message publisher.</returns>
        public static Abstractions.IMessageReceiver Create(
            string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock)
        {
            return new ServiceBusMessageReceiver(new MessageReceiver(connectionString, entityPath, receiveMode));
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Abstractions.Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = ConvertToMessage(await _messageReceiver
                        .ReceiveAsync(TimeSpan.FromSeconds(1))
                        .ConfigureAwait(false));

                    try
                    {
                        await handleMessages(new Abstractions.Message[] { message }, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        await RejectAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await _messageReceiver.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task KeepAliveAsync(Abstractions.Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.RenewLockAsync((string)message.SystemProperties[LockTokenProperty]).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ConfirmAsync(IEnumerable<Abstractions.Message> messages, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(messages.Select(m =>
            {
                return _messageReceiver.CompleteAsync((string)m.SystemProperties[LockTokenProperty]);
            })).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RejectAsync(Abstractions.Message message, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.AbandonAsync((string)message.SystemProperties[LockTokenProperty]).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeadLetterAsync(Abstractions.Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            await _messageReceiver.DeadLetterAsync((string)message.SystemProperties[LockTokenProperty], reason, errorDescription).ConfigureAwait(false);
        }

        private Abstractions.Message ConvertToMessage(Message message)
        {
            return new Abstractions.Message(
                id: message.MessageId,
                content: message.Body,
                properties: message.UserProperties.ToDictionary(t => t.Key, t => t.Value),
                systemProperties: new Dictionary<string, object>
                {
                    { LockTokenProperty, message.SystemProperties.LockToken },
                    { DeliveryCountProperty, message.SystemProperties.DeliveryCount },
                });
        }
    }
}