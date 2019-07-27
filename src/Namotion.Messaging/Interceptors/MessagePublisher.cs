using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Interceptors
{
    internal class MessagePublisher<T> : IMessagePublisher<T>
    {
        private readonly IMessagePublisher _messagePublisher;

        public MessagePublisher(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }

        public Task SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return _messagePublisher.SendAsync(messages, cancellationToken);
        }

        public void Dispose()
        {
            _messagePublisher.Dispose();
        }
    }
}
