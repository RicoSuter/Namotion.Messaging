using Xunit;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Azure.ServiceBus;
using System.Threading.Tasks;

namespace Namotion.Messaging.Tests.Implementations
{
    public class ServiceBusMessagingTests : MessagingTestsBase
    {
        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ServiceBusMessagePublisher
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .AsPublisher<MyMessage>();
        }

        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ServiceBusMessageReceiver
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .AsReceiver<MyMessage>();
        }

        public override async Task<List<Message>> WhenSendingMessages_ThenMessagesWithPropertisShouldBeReceived()
        {
            var messages = await base.WhenSendingMessages_ThenMessagesWithPropertisShouldBeReceived();

            // Assert
            foreach (var message in messages)
            {
                Assert.Equal(1, message.SystemProperties["DeliveryCount"]);
                Assert.NotNull(message.Id);
            }

            return messages;
        }

        [Fact(Skip = "Not supported")]
        public override Task WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero()
        {
            return base.WhenRetrievingMessageCount_ThenCountIsGreaterOrEqualZero();
        }
    }
}
