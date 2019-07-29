using Xunit;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Azure.ServiceBus;

namespace Namotion.Messaging.Tests.Implementations
{
    public class ServiceBusMessagingTests : MessagingTestsBase
    {
        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return ServiceBusMessageReceiver
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .WithMessageType<MyMessage>();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return ServiceBusMessagePublisher
                .Create(configuration["ServiceBusConnectionString"], "myqueue")
                .WithMessageType<MyMessage>();
        }

        protected override void Validate(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Assert.Equal(1, message.SystemProperties["DeliveryCount"]);
                Assert.NotNull(message.Id);
            }
        }
    }
}
