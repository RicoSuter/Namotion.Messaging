using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

namespace Namotion.Messaging.Tests
{
    public class GZipCompressionTests : MessagingTestsBase
    {
        private InMemoryMessagePublisherReceiver _publisherReceiver;

        public GZipCompressionTests()
        {
            _publisherReceiver = InMemoryMessagePublisherReceiver.Create();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ((IMessagePublisher)_publisherReceiver)
                .AsPublisher<MyMessage>()
                .WithGZipCompression(CompressionLevel.NoCompression);
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ((IMessageReceiver)_publisherReceiver)
                .AsReceiver<MyMessage>()
                .WithGZipCompression();
        }

        [Fact(Skip = "Not supported")]
        public override Task WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero()
        {
            return base.WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero();
        }
    }
}
