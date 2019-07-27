using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Interceptors
{
    internal class MessageReceiver<T> : IMessageReceiver<T>
    {
        private readonly IMessageReceiver _messageReceiver;

        public MessageReceiver(IMessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver;
        }

        public Task ListenAsync(Func<IEnumerable<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ListenAsync(handleMessages, cancellationToken);
        }

        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ConfirmAsync(messages, cancellationToken);
        }

        public Task DeadLetterAsync(Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.DeadLetterAsync(message, reason, errorDescription, cancellationToken);
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken)
        {
            return _messageReceiver.GetMessageCountAsync(cancellationToken);
        }

        public Task KeepAliveAsync(Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.KeepAliveAsync(message, timeToLive, cancellationToken);
        }

        public Task RejectAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.RejectAsync(message, cancellationToken);
        }
    }
}
