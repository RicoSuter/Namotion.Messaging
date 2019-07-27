using Xunit;
using System;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Namotion.Messaging.Tests.Implementations
{
    public class ServiceBusMessagingTests : MessagingTestsBase
    {
        protected override IMessageReceiver CreateMessageReceiver(IConfiguration configuration)
        {
            return new Azure.ServiceBus.ServiceBusMessageReceiver(configuration["ServiceBusConnectionString"], "myqueue");
        }

        protected override IMessagePublisher CreateMessagePublisher(IConfiguration configuration)
        {
            return new Azure.ServiceBus.ServiceBusMessagePublisher(configuration["ServiceBusConnectionString"], "myqueue");
        }

        protected override QueueMessage CreateMessage(byte[] content)
        {
            var message = base.CreateMessage(content);
            message.Id = Guid.NewGuid().ToString();
            return message;
        }

        protected override void Validate(List<QueueMessage> messages)
        {
            foreach (var message in messages)
            {
                Assert.NotNull(message.Id);
            }
        }
    }
}
