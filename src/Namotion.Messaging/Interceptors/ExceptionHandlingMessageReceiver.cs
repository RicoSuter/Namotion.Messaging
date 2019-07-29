using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Interceptors
{
    internal class ExceptionHandlingMessageReceiver<T> : MessageReceiver<T>
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly ILogger _logger;

        public ExceptionHandlingMessageReceiver(IMessageReceiver messageReceiver, ILogger logger)
            : base(messageReceiver)
        {
            _messageReceiver = messageReceiver;
            _logger = logger;
        }

        public override Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
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
    }
}
