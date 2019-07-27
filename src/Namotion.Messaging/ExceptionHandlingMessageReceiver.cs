using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    public class ExceptionHandlingMessageReceiver : IMessageReceiver
    {
        private readonly IMessageReceiver _messageReceiver;

        public ExceptionHandlingMessageReceiver(IMessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver;
        }

        public Task ListenAsync(Func<IReadOnlyCollection<QueueMessage>, CancellationToken, Task> onMessageAsync, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ListenAsync(async (messages, ct) =>
            {
                try
                {
                    await onMessageAsync(messages, ct).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // TODO: Log exception
                    foreach (var message in messages)
                    {
                        await RejectAsync(message, ct).ConfigureAwait(false);
                    }
                }
            }, cancellationToken);
        }

        public Task ConfirmAsync(IReadOnlyCollection<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ConfirmAsync(messages, cancellationToken);
        }

        public Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.DeadLetterAsync(message, reason, errorDescription, cancellationToken);
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken)
        {
            return _messageReceiver.GetMessageCountAsync(cancellationToken);
        }

        public Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.KeepAliveAsync(message, timeToLive, cancellationToken);
        }

        public Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.RejectAsync(message, cancellationToken);
        }
    }
}
