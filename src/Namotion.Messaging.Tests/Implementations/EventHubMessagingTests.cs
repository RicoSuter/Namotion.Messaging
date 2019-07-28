using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;
using Namotion.Messaging.Azure.EventHub;

namespace Namotion.Messaging.Tests.Implementations
{
    public class EventHubMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return new EventHubMessagePublisher(configuration["EventHubConnectionString"])
                .WithMessageType<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return new EventHubMessageReceiver(
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
    }
}
