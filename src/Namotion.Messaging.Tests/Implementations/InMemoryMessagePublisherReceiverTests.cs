using Microsoft.Extensions.Configuration;

namespace Namotion.Messaging.Tests.Implementations
{
    public class InMemoryMessagePublisherReceiverTests : MessagingTestsBase
    {
        private InMemoryMessagePublisherReceiver _publisherReceiver;

        public InMemoryMessagePublisherReceiverTests()
        {
            _publisherReceiver = InMemoryMessagePublisherReceiver.Create();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ((IMessagePublisher)_publisherReceiver).AsPublisher<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ((IMessageReceiver)_publisherReceiver).AsReceiver<MyMessage>();
        }
    }
}
