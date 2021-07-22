using Namotion.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Storage.Internal
{
    internal class BlobMessagePublisher<T> : MessagePublisher<T>
    {
        internal const string StorageKey = "Namotion.Messaging.Storage.Key";
        internal const int StorageKeyLength = 67;

        private readonly IBlobContainer _blobContainer;
        private readonly int _maxMessageSize;

        public BlobMessagePublisher(IMessagePublisher messagePublisher, IBlobContainer blobContainer, int maxMessageSize)
            : base(messagePublisher)
        {
            _blobContainer = blobContainer;
            _maxMessageSize = maxMessageSize;
        }

        public override async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            messages = await Task.WhenAll(messages // TODO: Use batches here
                .Select(message =>
                {
                    if (message.Content.Length > _maxMessageSize)
                    {
                        return Task.Run(async () =>
                        {
                            var key = Guid.NewGuid().ToString();
                            using (var stream = await _blobContainer.OpenWriteAsync(key, cancellationToken))
                            {
                                await stream.WriteAsync(message.Content, 0, message.Content.Length);
                                var content = Encoding.UTF8.GetBytes(StorageKey + ":" + key);
                                return new Message(message.Id, content, message.Properties, message.SystemProperties, message.PartitionId);
                            }
                        });
                    }
                    else
                    {
                        return Task.FromResult(message);
                    }
                }));

            await base.PublishAsync(messages, cancellationToken);
        }
    }
}
