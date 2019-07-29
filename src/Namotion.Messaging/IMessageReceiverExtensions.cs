using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging
{
    public static class IMessageReceiverExtensions
    {
        public static IMessageReceiver WithDeadLettering(this IMessageReceiver messageReceiver, IMessagePublisher messagePublisher)
        {
            return new DeadLetterQueuePublisherReceiver<object>(messageReceiver, messagePublisher);
        }

        public static IMessageReceiver WithDeadLettering<T>(this IMessageReceiver<T> messageReceiver, IMessagePublisher<T> messagePublisher)
        {
            return new DeadLetterQueuePublisherReceiver<T>(messageReceiver, messagePublisher);
        }

        public static IMessageReceiver<T> WithMessageType<T>(this IMessageReceiver messageReceiver)
        {
            return new MessageReceiver<T>(messageReceiver);
        }
    }
}
