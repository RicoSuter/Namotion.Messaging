using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// An null message publisher.
    /// </summary>
    public class NullMessagePublisher : IMessagePublisher
    {
        /// <summary>
        /// Sends a batch of messages to the queue.
        /// </summary>
        public Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
