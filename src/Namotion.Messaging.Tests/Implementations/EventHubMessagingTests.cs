using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Tests.Implementations
{
    public class EventHubMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher CreateMessagePublisher(IConfiguration configuration)
        {
            return new Azure.EventHub.EventHubMessagePublisher(configuration["EventHubConnectionString"]);
        }

        protected override IMessageReceiver CreateMessageReceiver(IConfiguration configuration)
        {
            return new Azure.EventHub.EventHubMessageReceiver("myeventhub", "$Default", configuration["EventHubConnectionString"],
                configuration["EventHubStorageConnectionString"], "myeventhub");
        }
    }
}
