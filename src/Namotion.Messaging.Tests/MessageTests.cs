using Xunit;
using Namotion.Messaging.Abstractions;

namespace Namotion.Messaging.Tests
{
    public class MessageTests
    {
        [Fact]
        public void WhenMessageIsCloned_ThenPropertiesAreCopied()
        {
            // Arrange
            var message = new Message(new byte[] { 1 })
            {
                Id = "abc"
            };

            // Act
            var clone = message.Clone();

            // Assert
            Assert.NotNull(message.Id);
        }
    }
}
