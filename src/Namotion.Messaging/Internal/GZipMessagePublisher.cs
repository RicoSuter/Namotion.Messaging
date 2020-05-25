using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Internal
{
    internal class GZipMessagePublisher<T> : MessagePublisher<T>
    {
        private readonly CompressionLevel _compressionLevel;

        public GZipMessagePublisher(IMessagePublisher messagePublisher, CompressionLevel compressionLevel)
            : base(messagePublisher)
        {
            _compressionLevel = compressionLevel;
        }

        public override Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            var compressedMessages = messages
                .Select(message =>
                {
                    var compressedContent = Compress(message.Content, _compressionLevel);
                    return new Message(message.Id, compressedContent, message.Properties, message.SystemProperties, message.PartitionId);
                })
                .AsParallel()
                .AsOrdered()
                .ToList();

            return base.PublishAsync(compressedMessages, cancellationToken);
        }

        private static byte[] Compress(byte[] input, CompressionLevel compressionLevel)
        {
            using (var stream = new MemoryStream())
            {
                var length = BitConverter.GetBytes(input.Length);
                stream.Write(length, 0, 4);

                using (var compressionStream = new GZipStream(stream, compressionLevel))
                {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Flush();
                }

                return stream.ToArray();
            }
        }
    }
}
