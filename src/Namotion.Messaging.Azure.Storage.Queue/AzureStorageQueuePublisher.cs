using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Azure.Storage.Queue
{
    /// <summary>
    /// An Azure Storage Queue message publisher.
    /// </summary>
    public class AzureStorageQueuePublisher : IMessagePublisher
    {
        private readonly CloudQueue _queue;

        private AzureStorageQueuePublisher(CloudQueue queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Creates a new Azure Storage Queue message publisher.
        /// </summary>
        /// <param name="accountName">The account name.</param>
        /// <param name="storageKey">The storage key.</param>
        /// <param name="queueName">The queue name</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(string accountName, string storageKey, string queueName)
        {
            var account = new CloudStorageAccount(new StorageCredentials(accountName, storageKey), true);
            var client = account.CreateCloudQueueClient();
            return new AzureStorageQueuePublisher(client.GetQueueReference(queueName));
        }

        /// <summary>
        /// Creates a new Storage Queue message publisher.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher CreateFromConnectionString(string connectionString, string queueName)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudQueueClient();
            return new AzureStorageQueuePublisher(client.GetQueueReference(queueName));
        }

        /// <inheritdoc />
        public async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                var nativeMessage = ConvertToMessage(message);
                await _queue.AddMessageAsync(nativeMessage);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        private CloudQueueMessage ConvertToMessage(Message message)
        {
            if (!string.IsNullOrEmpty(message.Id))
            {
                var nativeMessage = new CloudQueueMessage(message.Id, Guid.NewGuid().ToString());
                nativeMessage.SetMessageContent2(message.Content);
                return nativeMessage;
            }
            else
            {
                return new CloudQueueMessage(message.Content);
            }
        }
    }
}
