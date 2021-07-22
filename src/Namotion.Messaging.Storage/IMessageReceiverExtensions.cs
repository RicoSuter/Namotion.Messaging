using Namotion.Messaging.Storage.Internal;
using Namotion.Storage;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessageReceiver"/> extension methods.
    /// </summary>
    public static class IMessageReceiverExtensions
    {
        /// <summary>
        /// Decompresses the received messages using GZip (requires GZip decompression on the publisher).
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="blobContainer">The blob storage container.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver WithLargeMessageStorage(this IMessageReceiver messageReceiver, IBlobContainer blobContainer)
        {
            return new BlobMessageReceiver<object>(messageReceiver, blobContainer);
        }

        /// <summary>
        /// Decompresses the received messages using GZip and returns the original content if decompression failed (requires GZip decompression on the publisher).
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="blobContainer">The blob storage container.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver<T> WithLargeMessageStorage<T>(this IMessageReceiver<T> messageReceiver, IBlobContainer blobContainer)
        {
            return new BlobMessageReceiver<T>(messageReceiver, blobContainer);
        }
    }
}
