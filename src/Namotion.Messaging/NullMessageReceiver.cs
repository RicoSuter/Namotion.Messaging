using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// An null message receiver.
    /// </summary>
    public class NullMessageReceiver : IMessageReceiver
    {
        /// <summary>
        /// Gets the count of messages waiting to be processed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="NotSupportedException">The method is not supported.</exception>
        /// <returns>The message count.</returns>
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<long>(0);
        }

        /// <summary>
        /// Receives messages and passes them to the <paramref name="handleMessages"/> callback.
        /// The task completes when the listener throws an exception or the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="handleMessages">The message handler callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Extends the message lock timeout on the given messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="timeToLive">The desired time to live.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Confirms the processing of messages and removes them from the queue.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Rejects messages and requeues them for later reprocessing.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the messages and moves them to the dead letter queue.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="errorDescription">The error description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
#pragma warning disable CS1998
        public async ValueTask DisposeAsync()
        {
        }
    }
}
