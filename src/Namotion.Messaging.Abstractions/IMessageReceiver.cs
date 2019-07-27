using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Abstractions
{
    public interface IMessageReceiver
    {
        Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default);

        Task ListenAsync(Func<IEnumerable<QueueMessage>, CancellationToken, Task> onMessageAsync, CancellationToken cancellationToken = default);

        Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default);

        Task ConfirmAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default);

        Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default);

        Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default);
    }
}
