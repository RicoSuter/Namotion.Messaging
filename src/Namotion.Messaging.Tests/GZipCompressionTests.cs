using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Azure.ServiceBus;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

namespace Namotion.Messaging.Tests
{
    public class GZipCompressionTests : MessagingTestsBase
    {
        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ServiceBusMessageReceiver
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .WithMessageType<MyMessage>()
                .WithGZipCompression();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ServiceBusMessagePublisher
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .WithMessageType<MyMessage>()
                .WithGZipCompression(CompressionLevel.NoCompression);
        }

        [Fact(Skip = "Not supported")]
        public override Task WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero()
        {
            return base.WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero();
        }
    }
}
