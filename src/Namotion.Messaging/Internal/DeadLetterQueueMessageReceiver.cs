using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Internal
{
    internal class DeadLetterQueueMessageReceiver<T> : MessageReceiver<T>
    {
        private readonly IMessagePublisher _messagePublisher;

        public DeadLetterQueueMessageReceiver(IMessageReceiver messageReceiver, IMessagePublisher messagePublisher)
            : base(messageReceiver)
        {
            _messagePublisher = messagePublisher;
        }

        public async override Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            // TODO: Does this require better exception handling?
            await _messagePublisher.PublishAsync(messages, cancellationToken).ConfigureAwait(false);
            await ConfirmAsync(messages, cancellationToken).ConfigureAwait(false);
        }
    }
}
