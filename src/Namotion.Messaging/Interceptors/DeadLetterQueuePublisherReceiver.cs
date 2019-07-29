using Namotion.Messaging.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Interceptors
{
    internal class DeadLetterQueuePublisherReceiver<T> : MessageReceiver<T>
    {
        private readonly IMessagePublisher _messagePublisher;

        public DeadLetterQueuePublisherReceiver(IMessageReceiver messageReceiver, IMessagePublisher messagePublisher) 
            : base(messageReceiver)
        {
            _messagePublisher = messagePublisher;
        }

        public async override Task DeadLetterAsync(Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            // TODO: How to ensure transaction here?
            await _messagePublisher.SendAsync(message, cancellationToken);
            await this.ConfirmAsync(message, cancellationToken);
        }
    }
}
