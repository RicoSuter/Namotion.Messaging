using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// An in-memory message publisher and receiver implementation.
    /// </summary>
    public class InMemoryMessagePublisherReceiver : IMessagePublisher, IMessageReceiver
    {
        private long _count = 0;
        private object _lock = new object();
        private readonly bool _awaitProcessing;
        private readonly List<Message> _deadLetterMessages = new List<Message>();

        private Dictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken> funcs =
            new Dictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken>();

        public InMemoryMessagePublisherReceiver(bool awaitProcessing = false)
        {
            _awaitProcessing = awaitProcessing;
        }

        /// <summary>
        /// Gets the dead lettered messages.
        /// </summary>
        public IEnumerable<Message> DeadLetterMessages => _deadLetterMessages;

        /// <inheritdoc/>
        public async Task SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            IEnumerable<Task> tasks;
            lock (_lock)
            {
                _count += messages.Count();
                tasks = funcs.Select(f => Task.Run(() => f.Key(messages.ToArray(), f.Value)));
            }

            var task = Task.WhenAll(tasks)
                .ContinueWith(t =>
                {
                    lock (_lock)
                    {
                        _count -= messages.Count();
                    }
                });

            if (_awaitProcessing)
            {
                await task.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    funcs[handleMessages] = cancellationToken;
                }

                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_lock)
                {
                    funcs.Remove(handleMessages);
                }
            }
        }

        /// <inheritdoc/>
        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RejectAsync(Message message, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            await SendAsync(new Message[] { message }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(Message message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _deadLetterMessages.Add(message);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_count);
        }

        /// <inheritdoc/>
        public Task KeepAliveAsync(Message message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
