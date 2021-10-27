using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Azure.EventHub;
using Xunit;

namespace Namotion.Messaging.Tests.Implementations
{
    public class EventHubMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return EventHubMessagePublisher
                .Create(configuration["EventHubConnectionString"], "myeventhub")
                .AsPublisher<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return EventHubMessageReceiver
                .Create("$Default", configuration["EventHubConnectionString"], "myeventhub",
                    configuration["EventHubStorageConnectionString"], "myeventhub")
                .AsReceiver<MyMessage>();
        }

        protected override int GetMessageCount()
        {
            return 1000;
        }

        [Fact(Skip = "Not supported")]
        public override Task WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero()
        {
            return base.WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero();
        }
    }
}
