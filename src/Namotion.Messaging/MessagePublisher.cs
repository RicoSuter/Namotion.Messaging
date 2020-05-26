using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// A message publisher proxy to override or wrap methods.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class MessagePublisher<T> : IMessagePublisher<T>
    {
        private readonly IMessagePublisher _messagePublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePublisher{T}"/> class.
        /// </summary>
        /// <param name="messagePublisher">The message publisher to proxy.</param>
        public MessagePublisher(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }

        /// <inheritdoc/>
        public virtual Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return _messagePublisher.PublishAsync(messages, cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _messagePublisher.Dispose();
        }
    }
}
