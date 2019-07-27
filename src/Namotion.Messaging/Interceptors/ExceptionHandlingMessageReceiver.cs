using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Interceptors
{
    internal class ExceptionHandlingMessageReceiver : IMessageReceiver
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly ILogger _logger;

        public ExceptionHandlingMessageReceiver(IMessageReceiver messageReceiver, ILogger logger)
        {
            _messageReceiver = messageReceiver;
            _logger = logger;
        }

        public Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return _messageReceiver.ListenAsync(async (messages, ct) =>
            {
                try
                {
                    await handleMessages(messages, ct).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "An error occurred while processing a message.");
                    //foreach (var message in messages)
                    //{
                    //    await RejectAsync(message, ct).ConfigureAwait(false);
                    //}
                }
            }, cancellationToken);
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
