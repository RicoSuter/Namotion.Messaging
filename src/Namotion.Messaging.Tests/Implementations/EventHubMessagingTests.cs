using System.Threading.Tasks;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Azure.EventHub;
using Xunit;

namespace Namotion.Messaging.Tests.Implementations
{
    public class EventHubMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return EventHubMessagePublisher
                .Create(configuration["EventHubConnectionString"])
                .WithMessageType<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return EventHubMessageReceiver
                .CreateFromEventProcessorHost(
                    new EventProcessorHost("myeventhub",
                        "$Default",
                        configuration["EventHubConnectionString"],
                        configuration["EventHubStorageConnectionString"],
                        "myeventhub"),
                    new EventProcessorOptions { PrefetchCount = 500, MaxBatchSize = 100 })
                .WithMessageType<MyMessage>();
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
