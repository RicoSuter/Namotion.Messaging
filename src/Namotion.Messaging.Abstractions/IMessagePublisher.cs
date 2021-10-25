using Namotion.Messaging.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// Publishes messages to a message queue, broker or data ingestion system.
    /// </summary>
    /// <typeparam name="T">The marker type for the dependency injection system.</typeparam>
    public interface IMessagePublisher<T> : IMessagePublisher { }

    /// <summary>
    /// Publishes messages to a message queue, broker or data ingestion system.
    /// </summary>
    public interface IMessagePublisher : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Sends a batch of messages to the queue.
        /// </summary>
        /// <exception cref="MessagePublishingFailedException">Some messages could not be published.</exception>
        Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default);
    }
}
