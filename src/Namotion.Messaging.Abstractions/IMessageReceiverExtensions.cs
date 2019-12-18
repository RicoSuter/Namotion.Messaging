using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
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
            _ = message ?? throw new ArgumentNullException(nameof(message));

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
            _ = message ?? throw new ArgumentNullException(nameof(message));

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
            _ = message ?? throw new ArgumentNullException(nameof(message));

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
            _ = message ?? throw new ArgumentNullException(nameof(message));

            return messageReceiver.DeadLetterAsync(new Message[] { message }, reason, errorDescription, cancellationToken);
        }

        /// <summary>
        /// Receives messages and passes them to the <paramref name="handleMessages"/> callback.
        /// The task does not complete until the <paramref name="cancellationToken"/> is cancelled.
        /// Exceptions of the listener are handled and retried.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="handleMessages">The function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ListenWithRetryAsync(this IMessageReceiver messageReceiver,
            Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, 
            CancellationToken cancellationToken = default)
        {
            return ListenWithRetryAsync(messageReceiver, handleMessages, NullLogger.Instance, cancellationToken);
        }

        /// <summary>
        /// Receives messages and passes them to the <paramref name="handleMessages"/> callback.
        /// The task does not complete until the <paramref name="cancellationToken"/> is cancelled.
        /// Exceptions of the listener are handled and retried.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="handleMessages">The function.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static async Task ListenWithRetryAsync(this IMessageReceiver messageReceiver,
            Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages,
            ILogger logger, CancellationToken cancellationToken = default)
        {
            _ = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await messageReceiver.ListenAsync(handleMessages, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogError(new EventId(), e, "An error occured while listening for messages.");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
    }
}
