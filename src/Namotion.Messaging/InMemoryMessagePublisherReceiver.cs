using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _shutdownTokenSource;
        private readonly Task _backgroundTask;

        private readonly AsyncAutoResetEvent _processingTriggerEvent;
        private readonly AsyncManualResetEvent _notProcessingEvent;

        private ConcurrentQueue<Message> _queue;
        private readonly Collection<Message> _deadLetterQueue;

        private readonly ConcurrentDictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken> _consumers;

        /// <summary>
        /// Creates a new in-memory message publisher and receiver which sends messages without blocking.
        /// </summary>
        /// <returns>The publisher and receiver.</returns>
        public static InMemoryMessagePublisherReceiver Create(int maxBatchSize = 50, ILogger logger = null)
        {
            return new InMemoryMessagePublisherReceiver(maxBatchSize, logger ?? NullLogger.Instance);
        }

        internal InMemoryMessagePublisherReceiver(int maxBatchSize = 50, ILogger logger = null)
        {
            if (maxBatchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), maxBatchSize,
                    "The maximum batch size has to be positive number.");
            }

            _logger = logger ?? NullLogger.Instance;
            _shutdownTokenSource = new CancellationTokenSource();

            _processingTriggerEvent = new AsyncAutoResetEvent(false);
            _notProcessingEvent = new AsyncManualResetEvent(true);

            _queue = new ConcurrentQueue<Message>();
            _deadLetterQueue = new Collection<Message>();
            _consumers = new ConcurrentDictionary<Func<IReadOnlyCollection<Message>, CancellationToken, Task>, CancellationToken>();

            _backgroundTask = StartBackgroundTaskAsync(maxBatchSize);
        }

        /// <inheritdoc/>
        public Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                foreach (var message in messages)
                {
                    _queue.Enqueue(message);
                }

                _processingTriggerEvent.Set();
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            if (!_consumers.TryAdd(handleMessages, cancellationToken))
            {
                throw new InvalidOperationException("The specified consumer is already listening.");
            }

            _processingTriggerEvent.Set();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _consumers.TryRemove(handleMessages, out _);
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
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            await PublishAsync(messages, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            lock (_deadLetterQueue)
            {
                foreach (var message in messages)
                {
                    _deadLetterQueue.Add(message);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the dead lettered messages.
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public IEnumerable<Message> DeadLetterMessages
        {
            get
            {
                lock (_deadLetterQueue)
                {
                    return _deadLetterQueue.ToArray();
                }
            }
        }

        /// <summary>
        /// Purges all messages from the dead letter queue.
        /// </summary>
        /// <returns>The purged messages.</returns>
        public IEnumerable<Message> PurgeDeadLetterQueue()
        {
            lock (_deadLetterQueue)
            {
                var messages = DeadLetterMessages;
                _deadLetterQueue.Clear();
                return messages;
            }
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

        /// <summary>
        /// Purges all internal structures.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task">task</see> representing the asynchronous operation.</returns>
        public async Task PurgeQueueAsync(CancellationToken cancellationToken = default)
        {
            _queue = new ConcurrentQueue<Message>();
            await _notProcessingEvent.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Purges both message and dead-letter queues.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task">task</see> representing the asynchronous operation.</returns>
        public Task PurgeAsync(CancellationToken cancellationToken = default)
        {
            PurgeDeadLetterQueue();
            return PurgeQueueAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DisposeAsync()
        {
            _shutdownTokenSource.Cancel();
            try
            {
                await _backgroundTask.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        private Task StartBackgroundTaskAsync(int maxBatchSize)
        {
            return Task.Run(async () =>
            {
                while (!_shutdownTokenSource.Token.IsCancellationRequested)
                {
                    await _processingTriggerEvent.WaitAsync(_shutdownTokenSource.Token);

                    if (_consumers.Any())
                    {
                        _notProcessingEvent.Reset();
                        try
                        {
                            while (!_shutdownTokenSource.Token.IsCancellationRequested)
                            {
                                var messages = new List<Message>();
                                while (messages.Count < maxBatchSize && _queue.TryDequeue(out var msg))
                                {
                                    messages.Add(msg);
                                }

                                if (messages.Count != 0)
                                {
                                    var tasks = _consumers.Select(f => Task.Run(() =>
                                    {
                                        try
                                        {
                                            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_shutdownTokenSource.Token, f.Value);
                                            return f.Key(messages, cancellationSource.Token);
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
                                    await Task.WhenAll(tasks).ConfigureAwait(false);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            _notProcessingEvent.Set();
                        }
                    }
                }
            }, _shutdownTokenSource.Token);
        }
    }
}
