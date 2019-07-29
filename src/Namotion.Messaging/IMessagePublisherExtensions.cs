using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessagePublisher"/> extension methods.
    /// </summary>
    public static class IMessagePublisherExtensions
    {
        /// <summary>
        /// Adds a generic message type to the message publisher.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <returns>The wrapped message publisher.</returns>
        public static IMessagePublisher<T> WithMessageType<T>(this IMessagePublisher messagePublisher)
        {
            return new MessagePublisher<T>(messagePublisher);
        }
    }
}
