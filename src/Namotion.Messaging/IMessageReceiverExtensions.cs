using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Internal;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessageReceiver"/> extension methods.
    /// </summary>
    public static class IMessageReceiverExtensions
    {
        /// <summary>
        /// Adds custom dead lettering to thegiven message reciever.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="messagePublisher">The dead letter message publisher.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver WithDeadLettering(this IMessageReceiver messageReceiver, IMessagePublisher messagePublisher)
        {
            return new DeadLetterQueueMessageReceiver<object>(messageReceiver, messagePublisher);
        }

        /// <summary>
        /// Adds custom dead lettering to the given message reciever.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="messagePublisher">The dead letter message publisher.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver<T> WithDeadLettering<T>(this IMessageReceiver<T> messageReceiver, IMessagePublisher<T> messagePublisher)
        {
            return new DeadLetterQueueMessageReceiver<T>(messageReceiver, messagePublisher);
        }

        /// <summary>
        /// Decompresses the received messages using GZip (requires GZip decompression on the publisher).
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver WithGZipCompression(this IMessageReceiver messageReceiver)
        {
            return new GZipMessageReceiver<object>(messageReceiver);
        }

        /// <summary>
        /// Decompresses the received messages using GZip and returns the original content if decompression failed (requires GZip decompression on the publisher).
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver<T> WithGZipCompression<T>(this IMessageReceiver<T> messageReceiver)
        {
            return new GZipMessageReceiver<T>(messageReceiver);
        }

        /// <summary>
        /// Adds a generic message type to the message receiver.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver<T> AsReceiver<T>(this IMessageReceiver messageReceiver)
        {
            return new MessageReceiver<T>(messageReceiver);
        }

        /// <summary>
        /// Adds a generic message type to the message receiver.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver AsReceiver(this IMessageReceiver messageReceiver)
        {
            return messageReceiver;
        }
    }
}
