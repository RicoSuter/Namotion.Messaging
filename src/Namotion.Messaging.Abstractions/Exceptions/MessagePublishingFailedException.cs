using System;

namespace Namotion.Messaging.Abstractions.Exceptions
{
    public class MessagePublishingFailedException : Exception
    {
        public Message[] FailedMessages { get; }

        public MessagePublishingFailedException(Message[] failedMessages, string message, Exception innerException)
            : base(message, innerException)
        {
            FailedMessages = failedMessages;
        }
    }
}
