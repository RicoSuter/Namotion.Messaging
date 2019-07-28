namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// A generic message with deserialized content to be used in all queue implementations.
    /// </summary>
    public class ObjectMessage<T> : Message
    {
        /// <summary>
        /// Creates an instance of <see cref="ObjectMessage{T}"/>.
        /// </summary>
        /// <param name="content">The message content.</param>
        public ObjectMessage(byte[] content, T obj)
            : base(content)
        {
            Object = obj;
        }

        /// <summary>
        /// Gets the JSON deserialzed content.
        /// </summary>
        public T Object { get; }
    }
}
