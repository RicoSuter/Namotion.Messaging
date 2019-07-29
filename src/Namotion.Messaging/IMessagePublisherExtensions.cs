using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging
{
    public static class IMessagePublisherExtensions
    {
        /// <summary>
        /// Adds a generic type marker to the given message publisher.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher<T> WithMessageType<T>(this IMessagePublisher messagePublisher)
        {
            return new MessagePublisher<T>(messagePublisher);
        }
    }
}
