using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    public static class IMessageReceiverExtensions
    {
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
    }
}
