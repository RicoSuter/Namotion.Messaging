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
    /// Extension methods for <see cref="IEnumerable{Message}">.</see>
    /// </summary>
    public static class MessageEnumerableExtensions
    {
        /// <summary>
        /// Processes a batch of messages in parallel using in-memory partitions.
        /// It divides a "native" Event Hub partition message batch into smaller in-memory partitions within the batch. 
        /// In some scenarios this is required to have enough parallelization to fully use the available CPU and other resources.
        /// Messages are not confirmed/rejected and exceptions must be handled manually in <paramref name="processMessage"/>.
        /// </summary>
        /// <typeparam name="TPartitionKey">The partition property key type.</typeparam>
        /// <param name="messages">The messages.</param>
        /// <param name="partitionKeySelector">The message property partition key selector.</param>
        /// <param name="processMessage">The message processor function.</param>
        /// <param name="partitionParallelization">The number of parallel partition processing threads (thread pool).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ProcessByPartitionKeyAsync<TPartitionKey>(
            this IEnumerable<Message> messages,
            Func<Message, TPartitionKey> partitionKeySelector,
            Func<IEnumerable<Message>, CancellationToken, Task> processMessage,
            int partitionParallelization = 4,
            CancellationToken cancellationToken = default)
        {
            return ProcessByPartitionKeyAsync(messages, null, partitionKeySelector, processMessage, partitionParallelization, cancellationToken);
        }

        /// <summary>
        /// Processes a batch of messages in parallel using in-memory partitions.
        /// It divides a "native" Event Hub partition message batch into smaller in-memory partitions within the batch. 
        /// In some scenarios this is required to have enough parallelization to fully use the available CPU and other resources.
        /// Messages are not confirmed/rejected and exceptions must be handled manually in <paramref name="processPartitionMessages"/>.
        /// </summary>
        /// <typeparam name="TMessage">The deserialized message type.</typeparam>
        /// <typeparam name="TPartitionKey">The partition property key type.</typeparam>
        /// <param name="messages">The messages.</param>
        /// <param name="transform">The message deserialization function.</param>
        /// <param name="partitionKeySelector">The message property partition key selector.</param>
        /// <param name="processPartitionMessages">The message processor function.</param>
        /// <param name="partitionParallelization">The number of parallel partition processing threads (thread pool).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static async Task ProcessByPartitionKeyAsync<TMessage, TPartitionKey>(
            this IEnumerable<Message> messages,
            Func<Message, TMessage> transform,
            Func<TMessage, TPartitionKey> partitionKeySelector,
            Func<IEnumerable<TMessage>, CancellationToken, Task> processPartitionMessages,
            int partitionParallelization = 4,
            CancellationToken cancellationToken = default)
        {
            var deserializedMessages = transform != null ? messages
                .Select(m => transform(m))
                .AsParallel()
                    .AsOrdered()
                .ToArray() : messages.Cast<TMessage>().ToArray();

            var batchPartitionsQueue = new ConcurrentQueue<TMessage[]>(
                deserializedMessages
                    .GroupBy(partitionKeySelector)
                    .Select(g => g.ToArray())
                    .ToArray());

            var tasks = Enumerable
                .Range(0, partitionParallelization)
                .Select(i => Task.Run(async () =>
                {
                    while (batchPartitionsQueue.TryDequeue(out var batchPartition))
                    {
                        await processPartitionMessages(batchPartition, cancellationToken);
                    }
                }, cancellationToken));

            await Task.WhenAll(tasks);
        }
    }
}
