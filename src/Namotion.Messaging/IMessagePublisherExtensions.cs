using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Interceptors;

namespace Namotion.Messaging
{
    public static class IMessagePublisherExtensions
    {
        public static IMessagePublisher<T> For<T>(this IMessagePublisher messagePublisher)
        {
            return new MessagePublisher<T>(messagePublisher);
        }
    }
}
