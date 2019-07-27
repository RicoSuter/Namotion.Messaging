using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// Publishes messages to a message queue, broker or data ingestion system.
    /// </summary>
    public interface IMessagePublisher : IDisposable
    {
        /// <summary>
        /// Puts a batch of messages to the end of the queue.
        /// </summary>
        Task SendAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default);
    }
}
