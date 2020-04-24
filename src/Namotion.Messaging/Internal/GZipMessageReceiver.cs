using Namotion.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Internal
{
    internal class GZipMessageReceiver<T> : MessageReceiver<T>
    {
        public GZipMessageReceiver(IMessageReceiver messageReceiver)
            : base(messageReceiver)
        {
        }

        public override Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            return base.ListenAsync(async (messages, ct) =>
            {
                var decompressedMessages = messages
                    .Select(message =>
                    {
                        try
                        {
                            var decompressedContent = Decompress(message.Content);
                            return new Message(message.Id, decompressedContent, message.Properties, message.SystemProperties, message.PartitionId);
                        }
                        catch
                        {
                            return message;
                        }
                    })
                    .AsParallel()
                    .AsOrdered()
                    .ToList();

                await handleMessages(decompressedMessages, ct);
            }, cancellationToken);
        }

        private static byte[] Decompress(byte[] input)
        {
            using (var stream = new MemoryStream(input))
            {
                var lengthBytes = new byte[4];
                stream.Read(lengthBytes, 0, 4);

                var length = BitConverter.ToInt32(lengthBytes, 0);
                using (var decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    var result = new byte[length];
                    decompressionStream.Read(result, 0, length);
                    return result;
                }
            }
        }
    }
}
