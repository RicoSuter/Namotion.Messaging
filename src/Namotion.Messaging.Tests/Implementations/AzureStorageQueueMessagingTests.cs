using Xunit;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Azure.Storage.Queue;

namespace Namotion.Messaging.Tests.Implementations
{
    public class AzureStorageQueueMessagingTests : MessagingTestsBase
    {
        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return AzureStorageQueueReceiver
                .CreateFromConnectionString(configuration["EventHubStorageConnectionString"], "myqueue")
                .WithMessageType<MyMessage>();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return AzureStorageQueuePublisher
                .CreateFromConnectionString(configuration["EventHubStorageConnectionString"], "myqueue")
                .WithMessageType<MyMessage>();
        }

        protected override void Validate(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Assert.Equal(1, message.SystemProperties["DequeueCount"]);
                Assert.NotNull(message.Id);
            }
        }
    }
}
