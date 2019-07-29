using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// A message receiver proxy to override or wrap methods.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class MessageReceiver<T> : IMessageReceiver<T>
    {
        private readonly IMessageReceiver _messageReceiver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceiver{T}"/> class.
        /// </summary>
        /// <param name="messageReceiver">The message receiver to proxy.</param>
        public MessageReceiver(IMessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver;
        }

        /// <inheritdoc/>
        public virtual Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ListenAsync(handleMessages, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ConfirmAsync(messages, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task DeadLetterAsync(Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.DeadLetterAsync(message, reason, errorDescription, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<long> GetMessageCountAsync(CancellationToken cancellationToken)
        {
            return _messageReceiver.GetMessageCountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task KeepAliveAsync(Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.KeepAliveAsync(message, timeToLive, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task RejectAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.RejectAsync(message, cancellationToken);
        }
    }
}
