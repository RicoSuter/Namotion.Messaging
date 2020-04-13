using Xunit;
using Namotion.Messaging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Namotion.Messaging.Tests
{
    public class MessageEnumerableExtensionsTests
    {
        [Fact]
        public async Task WhenMessagesAreProcessed_ThenPartitionIsProcessedInSequence()
        {
            for (int i = 0; i < 100; i++)
            {
                // Arrange
                var messages = new Message[]
                {
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "a" },
                        { "v", 1 }
                    }),
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "b" },
                        { "v", 1 }
                    }),
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "a" },
                        { "v", 2 }
                    }),
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "a" },
                        { "v", 3 }
                    }),
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "b" },
                        { "v", 2 }
                    }),
                    new Message(properties: new Dictionary<string, object>
                    {
                        { "pk", "a" },
                        { "v", 4 }
                    }),
                };

                var order = new Dictionary<string, List<int>>
                {
                    { "a", new List<int>() },
                    { "b", new List<int>() },
                };

                // Act
                await messages.ProcessByPartitionKeyAsync(p => p.Properties["pk"], (msgs, _) =>
                {
                    foreach (var message in msgs)
                    {
                        order[message.Properties["pk"].ToString()].Add((int)message.Properties["v"]);
                    }
                    return Task.CompletedTask;
                }, 4, CancellationToken.None);

                // Assert
                Assert.Equal(1, order["a"][0]);
                Assert.Equal(2, order["a"][1]);
                Assert.Equal(3, order["a"][2]);
                Assert.Equal(4, order["a"][3]);

                Assert.Equal(1, order["b"][0]);
                Assert.Equal(2, order["b"][1]);
            }
        }
    }
}
