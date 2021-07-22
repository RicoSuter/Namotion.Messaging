using Namotion.Messaging.Storage.Internal;
using Namotion.Storage;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessagePublisher"/> extension methods.
    /// </summary>
    public static class IMessagePublisherExtensions
    {
        /// <summary>
        /// Compresses the published messages using GZip (requires GZip compression on the receiver).
        /// </summary>
        /// <param name="messagePublisher">The message receiver.</param>
        /// <param name="blobContainer">The blob storage container.</param>
        /// <param name="maxMessageSize">The maximum message size to keep, otherwise the content is pushed to the blob storage container.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessagePublisher WithLargeMessageStorage(this IMessagePublisher messagePublisher, IBlobContainer blobContainer, int maxMessageSize)
        {
            return new BlobMessagePublisher<object>(messagePublisher, blobContainer, maxMessageSize);
        }

        /// <summary>
        /// Compresses the published messages using GZip (requires GZip compression on the receiver).
        /// </summary>
        /// <param name="messagePublisher">The message receiver.</param>
        /// <param name="blobContainer">The blob storage container.</param>
        /// <param name="maxMessageSize">The maximum message size to keep, otherwise the content is pushed to the blob storage container.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessagePublisher<T> WithLargeMessageStorage<T>(this IMessagePublisher<T> messagePublisher, IBlobContainer blobContainer, int maxMessageSize)
        {
            return new BlobMessagePublisher<T>(messagePublisher, blobContainer, maxMessageSize);
        }
    }
}
