using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    public static class IMessagePublisherExtensions
    {
        /// <summary>
        /// Sends a single message to the queue.
        /// </summary>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task SendAsync(this IMessagePublisher messagePublisher, QueueMessage message, CancellationToken cancellationToken = default)
        {
            return messagePublisher.SendAsync(new QueueMessage[] { message }, cancellationToken);
        }
    }
}
