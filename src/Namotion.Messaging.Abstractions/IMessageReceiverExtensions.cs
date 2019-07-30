using System;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// <see cref="IMessagePublisher"/> extension methods.
    /// </summary>
    public static class IMessageReceiverExtensions
    {
        /// <summary>
        /// Extends the message lock timeout on the given message.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="message">The message.</param>
        /// <param name="timeToLive">The desired time to live.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task KeepAliveAsync(this IMessageReceiver messageReceiver, Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return messageReceiver.KeepAliveAsync(new Message[] { message }, timeToLive, cancellationToken);
        }

        /// <summary>
        /// Confirms the processing of a single message and removes it from the queue.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="message">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ConfirmAsync(this IMessageReceiver messageReceiver, Message message, CancellationToken cancellationToken = default)
        {
            return messageReceiver.ConfirmAsync(new Message[] { message }, cancellationToken);
        }

        /// <summary>
        /// Rejects a message and requeues it for later reprocessing.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task RejectAsync(this IMessageReceiver messageReceiver, Message message, CancellationToken cancellationToken = default)
        {
            return messageReceiver.RejectAsync(new Message[] { message }, cancellationToken);
        }

        /// <summary>
        /// Removes the message and moves it to the dead letter queue.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="message">The message.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="errorDescription">The error description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task DeadLetterAsync(this IMessageReceiver messageReceiver, Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return messageReceiver.DeadLetterAsync(new Message[] { message }, reason, errorDescription, cancellationToken);
        }
    }
}
