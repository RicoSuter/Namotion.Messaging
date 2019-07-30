using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// Receives messages from a message queue, broker or data ingestion system.
    /// </summary>
    /// <typeparam name="T">The marker type for the dependency injection system.</typeparam>
    public interface IMessageReceiver<T> : IMessageReceiver { }

    /// <summary>
    /// Receives messages from a message queue, broker or data ingestion system.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Gets the count of messages waiting to be processed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="NotSupportedException">The method is not supported.</exception>
        /// <returns>The message count.</returns>
        Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives messages and passes them to the <paramref name="handleMessages"/> callback.
        /// The task does not complete until the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="handleMessages">The message handler callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends the message lock timeout on the given messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="timeToLive">The desired time to live.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms the processing of messages and removes them from the queue.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rejects messages and requeues them for later reprocessing.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the messages and moves them to the dead letter queue.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="errorDescription">The error description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default);
    }
}
