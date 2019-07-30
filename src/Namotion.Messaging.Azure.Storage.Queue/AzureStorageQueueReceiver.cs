using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Azure.Storage.Queue
{
    /// <summary>
    /// An Azure Storage Queue message receiver.
    /// </summary>
    public class AzureStorageQueueReceiver : IMessageReceiver
    {
        private static readonly TimeSpan RetryAfterEmptyResult = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan RetryAfterException = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RejectDelay = TimeSpan.FromSeconds(1);

        private const string DequeueCountProperty = "DequeueCount";
        private const string PopReceiptProperty = "PopReceipt";
        private const string NativeMessageProperty = "NativeMessage";

        private readonly CloudQueue _queue;
        private readonly int _maxBatchSize;

        private AzureStorageQueueReceiver(CloudQueue queue, int maxBatchSize)
        {
            _queue = queue;
            _maxBatchSize = maxBatchSize;
        }

        /// <summary>
        /// Creates a new Storage Queue message publisher.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="maxBatchSize">The maximum batch size (default: 4).</param>
        /// <returns>The message publisher.</returns>
        public static IMessageReceiver Create(string accountName, string storageKey, string queueName, int maxBatchSize = 4)
        {
            var account = new CloudStorageAccount(new StorageCredentials(accountName, storageKey), true);
            var client = account.CreateCloudQueueClient();
            return new AzureStorageQueueReceiver(client.GetQueueReference(queueName), maxBatchSize);
        }

        /// <summary>
        /// Creates a new Storage Queue message publisher.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="maxBatchSize">The maximum batch size (default: 4).</param>
        /// <returns>The message publisher.</returns>
        public static IMessageReceiver CreateFromConnectionString(string connectionString, string queueName, int maxBatchSize = 4)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudQueueClient();
            return new AzureStorageQueueReceiver(client.GetQueueReference(queueName), maxBatchSize);
        }

        /// <inheritdoc/>
        public async Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            await _queue.FetchAttributesAsync().ConfigureAwait(false);
            return _queue.ApproximateMessageCount ?? 0;
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var batch = await _queue.GetMessagesAsync(_maxBatchSize, TimeSpan.FromSeconds(30), null, null, cancellationToken).ConfigureAwait(false);
                    if (batch != null && batch.Any())
                    {
                        await handleMessages(batch.Select(m => ConvertToMessage(m)).ToArray(), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(RetryAfterEmptyResult, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await Task.Delay(RetryAfterException, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public async Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            await Task.WhenAll(messages.Select(m =>
            {
                return _queue.DeleteMessageAsync(m.Id, (string)m.SystemProperties[PopReceiptProperty]);
            })).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException" />
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                var nativeMessage = (CloudQueueMessage)m.Properties[NativeMessageProperty];
                return _queue.UpdateMessageAsync(nativeMessage, timeToLive ?? DefaultTimeToLive, MessageUpdateFields.Visibility);
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException" />
        public Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                var nativeMessage = (CloudQueueMessage)m.Properties[NativeMessageProperty];
                return _queue.UpdateMessageAsync(nativeMessage, RejectDelay, MessageUpdateFields.Visibility);
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException" />
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private Message ConvertToMessage(CloudQueueMessage message)
        {
            return new Message(
                id: message.Id,
                content: message.AsBytes,
                systemProperties: new Dictionary<string, object>
                {
                    { DequeueCountProperty, message.DequeueCount },
                    { PopReceiptProperty, message.PopReceipt },
                    { NativeMessageProperty, message },
                });
        }
    }
}
