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
        protected override IMessageReceiver CreateMessageReceiver(IConfiguration configuration)
        {
            return new ServiceBusMessageReceiver(configuration["ServiceBusConnectionString"], "myqueue");
        }

        protected override IMessagePublisher CreateMessagePublisher(IConfiguration configuration)
        {
            return new ServiceBusMessagePublisher(configuration["ServiceBusConnectionString"], "myqueue");
        }

        protected override Message CreateMessage(byte[] content)
        {
            var message = base.CreateMessage(content);
            message.Id = Guid.NewGuid().ToString();
            return message;
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
