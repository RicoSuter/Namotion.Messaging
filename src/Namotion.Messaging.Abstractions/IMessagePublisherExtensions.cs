using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    public static class IMessagePublisherExtensions
    {
        public static Task SendAsync(this IMessagePublisher messagePublisher, QueueMessage message, CancellationToken cancellationToken = default)
        {
            return messagePublisher.SendAsync(new QueueMessage[] { message }, cancellationToken);
        }
    }
}
