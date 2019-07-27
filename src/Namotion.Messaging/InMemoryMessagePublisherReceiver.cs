using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    public class InMemoryMessagePublisherReceiver : IMessagePublisher, IMessageReceiver
    {
        private long _count = 0;
        private object _lock = new object();
        private readonly bool _awaitProcessing;
        private readonly List<QueueMessage> _deadLetterMessages = new List<QueueMessage>();

        private Dictionary<Func<IReadOnlyCollection<QueueMessage>, CancellationToken, Task>, CancellationToken> funcs =
            new Dictionary<Func<IReadOnlyCollection<QueueMessage>, CancellationToken, Task>, CancellationToken>();

        public InMemoryMessagePublisherReceiver(bool awaitProcessing = false)
        {
            _awaitProcessing = awaitProcessing;
        }

        public IEnumerable<QueueMessage> DeadLetterMessages => _deadLetterMessages;

        public async Task PutMessagesAsync(IReadOnlyCollection<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            IEnumerable<Task> tasks;
            lock (_lock)
            {
                _count += messages.Count;
                tasks = funcs.Select(f => Task.Run(() => f.Key(messages, f.Value)));
            }

            var task = Task.WhenAll(tasks)
                .ContinueWith(t =>
                {
                    lock (_lock)
                    {
                        _count -= messages.Count;
                    }
                });

            if (_awaitProcessing)
            {
                await task.ConfigureAwait(false);
            }
        }

        public async Task ListenAsync(Func<IReadOnlyCollection<QueueMessage>, CancellationToken, Task> onMessageAsync, CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    funcs[onMessageAsync] = cancellationToken;
                }

                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    funcs.Remove(onMessageAsync);
                }
            }
        }

        public Task ConfirmAsync(IReadOnlyCollection<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            await PutMessagesAsync(new QueueMessage[] { message }, cancellationToken).ConfigureAwait(false);
        }

        public Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _deadLetterMessages.Add(message);
            }

            return Task.CompletedTask;
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_count);
        }

        public Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
