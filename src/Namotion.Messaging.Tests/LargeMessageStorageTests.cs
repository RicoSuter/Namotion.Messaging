using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;
using Namotion.Storage;
using Namotion.Storage.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Namotion.Messaging.Tests
{
    public class LargeMessageStorageTests : MessagingTestsBase
    {
        private IBlobContainer _blobContainer = new InMemoryBlobStorage().GetContainer("test");
        private InMemoryMessagePublisherReceiver _publisherReceiver;

        public LargeMessageStorageTests()
        {
            _publisherReceiver = InMemoryMessagePublisherReceiver.Create();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ((IMessagePublisher)_publisherReceiver)
                .WithMessageType<MyMessage>()
                .WithLargeMessageStorage(_blobContainer, 0);
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ((IMessageReceiver)_publisherReceiver)
                .WithMessageType<MyMessage>()
                .WithLargeMessageStorage(_blobContainer);
        }

        public override async Task<List<Message<MyMessage>>> WhenSendingJsonMessages_ThenMessagesShouldBeReceived()
        {
            var result = await base.WhenSendingJsonMessages_ThenMessagesShouldBeReceived();

            await Assert.ThrowsAsync<ContainerNotFoundException>(async () =>
            {
                await _blobContainer.ListAsync(string.Empty);
            });

            return result;
        }

        [Fact(Skip = "Not supported")]
        public override Task WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero()
        {
            return base.WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero();
        }
    }
}
