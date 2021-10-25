using Namotion.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Storage.Internal
{
    internal class BlobMessageReceiver<T> : MessageReceiver<T>
    {
        private readonly IBlobContainer _blobContainer;

        public BlobMessageReceiver(IMessageReceiver messageReceiver, IBlobContainer blobContainer)
            : base(messageReceiver)
        {
            _blobContainer = blobContainer;
        }

        public override Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return base.ListenAsync(async (messages, ct) =>
            {
                messages = await Task.WhenAll(messages // TODO: Use batches here
                    .Select(message =>
                    {
                        if (message.Content.Length == BlobMessagePublisher<T>.StorageKeyLength)
                        {
                            var content = Encoding.UTF8.GetString(message.Content);
                            if (content.StartsWith(BlobMessagePublisher<T>.StorageKey))
                            {
                                return Task.Run(async () => await CreateMessageWithBlobStorageAsync(message, content, cancellationToken));
                            }
                        }

                        return Task.FromResult(message);
                    }));

                await handleMessages(messages, ct);
            }, cancellationToken);
        }

        public override async Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            await base.ConfirmAsync(messages, cancellationToken);
            await Task.WhenAll(messages
                .Select(message =>
                {
                    if (message.Properties.ContainsKey(BlobMessagePublisher<T>.StorageKey))
                    {
                        return Task.Run(async () =>
                        {
                            var key = message.Properties[BlobMessagePublisher<T>.StorageKey].ToString();
                            await _blobContainer.DeleteAsync(key, cancellationToken);
                        });
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }));
        }

        private async Task<Message> CreateMessageWithBlobStorageAsync(Message message, string content, CancellationToken cancellationToken)
        {
            var key = content.Substring(BlobMessagePublisher<T>.StorageKey.Length + 1);
            using (var stream = await _blobContainer.OpenReadAsync(key, cancellationToken))
            {
                byte[] buffer = new byte[16 * 1024];
                using (var memoryStream = new MemoryStream())
                {
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, read);
                    }

                    var properties = message.Properties.ToDictionary(p => p.Key, p => p.Value);
                    properties[BlobMessagePublisher<T>.StorageKey] = key;

                    return new Message(message.Id, memoryStream.ToArray(), properties, message.SystemProperties, message.PartitionId);
                }
            }
        }
    }
}
