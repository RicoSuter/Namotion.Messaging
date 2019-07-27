using System.Collections.Generic;
using Xunit;
using Namotion.Messaging.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Namotion.Messaging.Tests
{
    public abstract class MessagingTestsBase
    {
        [Fact]
        public async Task WhenSendingMessages_ThenMessagesWithPropertisShouldBeReceived()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var count = 10;
            var content = Guid.NewGuid().ToByteArray();

            var publisher = CreateMessagePublisher(config);
            var receiver = CreateMessageReceiver(config);

            // Act
            var messages = new List<Message>();

            var listenCancellation = new CancellationTokenSource();
            var receiveCancellation = new CancellationTokenSource();
            var task = receiver.ListenAsync(async (msgs, ct) =>
            {
                foreach (var message in msgs
                    .Where(message => message.Content.SequenceEqual(content)))
                {
                    messages.Add(message);
                }

                if (messages.Count == count)
                {
                    receiveCancellation.Cancel();
                }

                await receiver.ConfirmAsync(msgs, ct);
            }, listenCancellation.Token);

            var stopwatch = Stopwatch.StartNew();
            await publisher.SendAsync(Enumerable.Range(1, count)
                .Select(i => CreateMessage(content))
                .ToList());

            await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30), receiveCancellation.Token));
            listenCancellation.Cancel();

            // Assert
            Assert.Equal(count, messages.Count);
            Validate(messages);
        }

        protected virtual Message CreateMessage(byte[] content)
        {
            // Arrange
            return new Message(content)
            {
                Properties =
                {
                    { "x-my-property", "hello" }
                }
            };
        }

        protected virtual void Validate(List<Message> messages)
        {
            // Assert
            foreach (var message in messages)
            {
                Assert.Equal("hello", message.Properties["x-my-property"]);
            }
        }

        protected abstract IMessageReceiver CreateMessageReceiver(IConfiguration configuration);

        protected abstract IMessagePublisher CreateMessagePublisher(IConfiguration configuration);
    }
}
