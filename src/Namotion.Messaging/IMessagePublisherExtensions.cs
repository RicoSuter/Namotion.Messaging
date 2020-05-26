using Namotion.Messaging.Internal;
using System.IO.Compression;

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
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessagePublisher WithGZipCompression(this IMessagePublisher messagePublisher, CompressionLevel compressionLevel)
        {
            return new GZipMessagePublisher<object>(messagePublisher, compressionLevel);
        }

        /// <summary>
        /// Compresses the published messages using GZip (requires GZip compression on the receiver).
        /// </summary>
        /// <param name="messagePublisher">The message receiver.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessagePublisher<T> WithGZipCompression<T>(this IMessagePublisher<T> messagePublisher, CompressionLevel compressionLevel)
        {
            return new GZipMessagePublisher<T>(messagePublisher, compressionLevel);
        }

        /// <summary>
        /// Adds a generic message type to the message publisher.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <returns>The wrapped message publisher.</returns>
        public static IMessagePublisher<T> AsPublisher<T>(this IMessagePublisher messagePublisher)
        {
            return new MessagePublisher<T>(messagePublisher);
        }

        /// <summary>
        /// Adds a generic message type to the message publisher.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <returns>The wrapped message publisher.</returns>
        public static IMessagePublisher AsPublisher(this IMessagePublisher messagePublisher)
        {
            return messagePublisher;
        }
    }
}
