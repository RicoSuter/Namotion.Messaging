using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Concurrent;
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
        private object _lock = new object();

        private readonly ConcurrentQueue<Message> _queue;
        private readonly CancellationTokenSource _cancellationSource;

        private readonly List<Message> _deadLetterMessages = new List<Message>();
        private readonly ILogger _logger;
        private Dictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken> funcs =
            new Dictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken>();

        private InMemoryMessagePublisherReceiver(ILogger logger)
        {
            _cancellationSource = new CancellationTokenSource();
            _queue = new ConcurrentQueue<Message>();
            _logger = logger;

            Task.Run(async () =>
            {
                while (!_cancellationSource.Token.IsCancellationRequested)
                {
                    if (funcs.Any() && _queue.TryDequeue(out var message))
                    {
                        IEnumerable<Task> tasks;
                        lock (_lock)
                        {
                            tasks = funcs.Select(f => Task.Run(() =>
                            {
                                try
                                {
                                    return f.Key(new[] { message }, f.Value);
                                }
                                catch (Exception e)
                                {
                                    if (!(e is TaskCanceledException))
                                    {
                                        _logger.LogError(e, "An error occurred in the in-memory message receiver.");
                                    }

                                    return Task.CompletedTask;
                                }
                            }));
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(250).ConfigureAwait(false);
                    }
                }
            }, _cancellationSource.Token);
        }

        /// <summary>
        /// Creates a new in-memory message publisher and receiver which sends messages without blocking.
        /// </summary>
        /// <returns>The publisher and receiver.</returns>
        public static InMemoryMessagePublisherReceiver Create(ILogger logger = null)
        {
            return new InMemoryMessagePublisherReceiver(logger ?? NullLogger.Instance);
        }

        /// <summary>
        /// Gets the dead lettered messages.
        /// </summary>
        public IEnumerable<Message> DeadLetterMessages => _deadLetterMessages;

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    _queue.Enqueue(message);
                }
            }

            return Task.CompletedTask;
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
            return Task.FromResult<long>(_queue.Count);
        }

        /// <inheritdoc/>
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cancellationSource.Cancel();
        }
    }
}
