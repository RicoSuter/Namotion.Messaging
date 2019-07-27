using Microsoft.Extensions.Logging;
using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Interceptors;

namespace Namotion.Messaging
{
    public static class IMessageReceiverExtensions
    {
        public static IMessageReceiver WithExceptionHandling(this IMessageReceiver messageReceiver, ILogger logger = null)
        {
            return new ExceptionHandlingMessageReceiver(messageReceiver, logger);
        }

        public static IMessageReceiver<T> For<T>(this IMessageReceiver messageReceiver)
        {
            return new MessageReceiver<T>(messageReceiver);
        }
    }
}
