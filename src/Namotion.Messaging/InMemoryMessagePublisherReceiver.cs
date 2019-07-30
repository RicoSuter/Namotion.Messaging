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

        private InMemoryMessagePublisherReceiver(bool awaitProcessing)
        {
            _awaitProcessing = awaitProcessing;
        }

        /// <summary>
        /// Creates a new in-memory message publisher and receiver which sends messages without blocking.
        /// </summary>
        /// <returns>The publisher and receiver.</returns>
        public static InMemoryMessagePublisherReceiver Create()
        {
            return new InMemoryMessagePublisherReceiver(false);
        }

        /// <summary>
        /// Creates a new in-memory message publisher and receiver which sends messages with blocking.
        /// </summary>
        /// <returns>The publisher and receiver.</returns>
        public static InMemoryMessagePublisherReceiver CreateWithMessageBlocking()
        {
            return new InMemoryMessagePublisherReceiver(true);
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
        public async Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            await SendAsync(messages, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _deadLetterMessages.AddRange(messages);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_count);
        }

        /// <inheritdoc/>
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
