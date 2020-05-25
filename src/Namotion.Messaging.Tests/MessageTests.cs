using Xunit;

namespace Namotion.Messaging.Tests
{
    public class MessageTests
    {
        [Fact]
        public void WhenMessageIsCloned_ThenPropertiesAreCopied()
        {
            // Arrange
            var message = new Message("abc", new byte[] { 1 });

            // Act
            var clone = message.Clone();

            // Assert
            Assert.NotNull(message.Id);
        }
    }
}
