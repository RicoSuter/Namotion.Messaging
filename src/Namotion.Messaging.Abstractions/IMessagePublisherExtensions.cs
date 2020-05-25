using System;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessagePublisher"/> extension methods.
    /// </summary>
    public static class IMessagePublisherExtensions
    {
        /// <summary>
        /// Sends a single message to the queue.
        /// </summary>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task PublishAsync(this IMessagePublisher messagePublisher, Message message, CancellationToken cancellationToken = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            return messagePublisher.PublishAsync(new Message[] { message }, cancellationToken);
        }
    }
}
