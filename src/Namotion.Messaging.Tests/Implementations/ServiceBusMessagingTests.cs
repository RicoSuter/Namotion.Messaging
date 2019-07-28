using Xunit;
using System;
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
            return new ServiceBusMessageReceiver(configuration["ServiceBusConnectionString"], "myqueue")
                .For<MyMessage>();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return new ServiceBusMessagePublisher(configuration["ServiceBusConnectionString"], "myqueue")
                .For<MyMessage>();
        }

        protected override void Validate(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Assert.Equal(1, message.DequeueCount);
                Assert.NotNull(message.Id);
            }
        }
    }
}
