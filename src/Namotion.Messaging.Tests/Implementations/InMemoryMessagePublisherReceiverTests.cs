using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Tests.Implementations
{
    public class InMemoryMessagePublisherReceiverTests : MessagingTestsBase
    {
        private InMemoryMessagePublisherReceiver _publisherReceiver;

        public InMemoryMessagePublisherReceiverTests()
        {
            _publisherReceiver = new InMemoryMessagePublisherReceiver();
        }

        protected override IMessagePublisher CreateMessagePublisher(IConfiguration configuration)
        {
            return _publisherReceiver;
        }

        protected override IMessageReceiver CreateMessageReceiver(IConfiguration configuration)
        {
            return _publisherReceiver;
        }
    }
}
