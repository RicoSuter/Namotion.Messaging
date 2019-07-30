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
        private readonly int _maxBatchSize;

        private ServiceBusMessageReceiver(MessageReceiver messageReceiver, int maxBatchSize)
        {
            _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(MessageReceiver));
            _maxBatchSize = maxBatchSize;
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a message receiver.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="maxMessageCount">The maximum message count (default: 1).</param>
        /// <returns>The message publisher.</returns>
        public static Abstractions.IMessageReceiver CreateFromMessageReceiver(MessageReceiver messageReceiver, int maxMessageCount = 1)
        {
            return new ServiceBusMessageReceiver(messageReceiver, maxMessageCount);
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="entityPath">The entity path.</param>
        /// <param name="receiveMode">The receive mode (default: PeekLock).</param>
        /// <param name="maxBatchSize">The maximum batch size (default: 1).</param>
        /// <returns>The message publisher.</returns>
        public static Abstractions.IMessageReceiver Create(
            string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock, int maxBatchSize = 1)
        {
            return new ServiceBusMessageReceiver(new MessageReceiver(connectionString, entityPath, receiveMode), maxBatchSize);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Abstractions.Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var messages = await _messageReceiver
                        .ReceiveAsync(_maxBatchSize, TimeSpan.FromSeconds(30))
                        .ConfigureAwait(false);

                    if (messages != null && messages.Any())
                    {
                        var abstractMessages = messages.Select(ConvertToMessage).ToArray();
                        try
                        {
                            await handleMessages(abstractMessages, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            // TODO: Improve, also what happens on cancel here?
                            foreach (var abstractMessage in abstractMessages)
                            {
                                await RejectAsync(abstractMessage, cancellationToken).ConfigureAwait(false);
                            }
                        }
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
        public Task KeepAliveAsync(Abstractions.Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.RenewLockAsync((string)message.SystemProperties[LockTokenProperty]);
        }

        /// <inheritdoc/>
        public Task ConfirmAsync(IEnumerable<Abstractions.Message> messages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.CompleteAsync(messages.Select(m => (string)m.SystemProperties[LockTokenProperty]));
        }

        /// <inheritdoc/>
        public Task RejectAsync(Abstractions.Message message, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.AbandonAsync((string)message.SystemProperties[LockTokenProperty]);
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(Abstractions.Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.DeadLetterAsync((string)message.SystemProperties[LockTokenProperty], reason, errorDescription);
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