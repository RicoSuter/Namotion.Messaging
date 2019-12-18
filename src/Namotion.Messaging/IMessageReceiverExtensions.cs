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
            return new DeadLetterQueuePublisherReceiver<object>(messageReceiver, messagePublisher);
        }

        /// <summary>
        /// Adds custom dead lettering to thegiven message reciever.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="messagePublisher">The dead letter message publisher.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver WithDeadLettering<T>(this IMessageReceiver<T> messageReceiver, IMessagePublisher<T> messagePublisher)
        {
            return new DeadLetterQueuePublisherReceiver<T>(messageReceiver, messagePublisher);
        }

        /// <summary>
        /// Adds a generic message type to the message receiver.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <returns>The wrapped message receiver.</returns>
        public static IMessageReceiver<T> WithMessageType<T>(this IMessageReceiver messageReceiver)
        {
            return new MessageReceiver<T>(messageReceiver);
        }
    }
}
