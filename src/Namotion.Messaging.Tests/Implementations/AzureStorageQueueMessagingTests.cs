using Xunit;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Azure.Storage.Queue;
using System.Threading.Tasks;

namespace Namotion.Messaging.Tests.Implementations
{
    public class AzureStorageQueueMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return AzureStorageQueuePublisher
                .CreateFromConnectionString(configuration["EventHubStorageConnectionString"], "myqueue")
                .AsPublisher<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return AzureStorageQueueReceiver
                .CreateFromConnectionString(configuration["EventHubStorageConnectionString"], "myqueue")
                .AsPublisher<MyMessage>();
        }

        [Fact(Skip = "Azure Storage Queue does not support properties.")]
        public override Task<List<Message>> WhenSendingMessages_ThenMessagesWithPropertisShouldBeReceived()
        {
            return base.WhenSendingMessages_ThenMessagesWithPropertisShouldBeReceived();
        }

        public override async Task<List<Message<MyMessage>>> WhenSendingJsonMessages_ThenMessagesShouldBeReceived()
        {
            var messages = await base.WhenSendingJsonMessages_ThenMessagesShouldBeReceived();

            // Assert
            foreach (var message in messages)
            {
                Assert.Equal(1, message.SystemProperties["DequeueCount"]);
                Assert.NotNull(message.Id);
            }

            return messages;
        }
    }
}
