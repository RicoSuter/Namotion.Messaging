using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Interceptors;

namespace Namotion.Messaging
{
    public static class IMessageReceiverExtensions
    {
        public static IMessageReceiver WithExceptionHandling<T>(this IMessageReceiver<T> messageReceiver, ILogger logger = null)
        {
            return new ExceptionHandlingMessageReceiver<T>(messageReceiver, logger);
        }

        public static IMessageReceiver WithExceptionHandling(this IMessageReceiver messageReceiver, ILogger logger = null)
        {
            return new ExceptionHandlingMessageReceiver<object>(messageReceiver, logger);
        }

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
