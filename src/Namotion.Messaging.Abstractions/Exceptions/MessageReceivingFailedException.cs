using System;

namespace Namotion.Messaging.Exceptions
{
    public class MessageReceivingFailedException : Exception
    {
        public MessageReceivingFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
