using System.Collections.Generic;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// A generic message with deserialized content to be used in all queue implementations.
    /// </summary>
    public class Message<T> : Message
    {
        /// <summary>
        /// Creates an instance of <see cref="Message{T}"/>.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="content">The message content.</param>
        /// <param name="deserializedObject">The deserialized object.</param>
        /// <param name="properties">The message user properties.</param>
        /// <param name="systemProperties">The message system properties.</param>
        /// <param name="partitionId">The partition ID.</param>
        public Message(
            string id = null,
            byte[] content = null,
            T deserializedObject = default,
            IReadOnlyDictionary<string, object> properties = null,
            IReadOnlyDictionary<string, object> systemProperties = null,
            string partitionId = null)
            : base(id, content, properties, systemProperties, partitionId)
        {
            Object = deserializedObject;
        }

        /// <summary>
        /// Gets the JSON deserialzed content.
        /// </summary>
        public T Object { get; }
    }

    /// <summary>
    /// A generic message to be used in all queue implementations.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Creates an instance of <see cref="Message"/>.
        /// </summary>
        /// <param name="content">The message content.</param>
        public Message(byte[] content)
            : this(null, content: content)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="Message"/>.
        /// </summary>
        /// <param name="id">The message Id.</param>
        /// <param name="content">The message content.</param>
        /// <param name="properties">The message user properties.</param>
        /// <param name="systemProperties">The message system properties.</param>
        /// <param name="partitionId">The partition ID.</param>
        public Message(
            string id = null,
            byte[] content = null,
            IReadOnlyDictionary<string, object> properties = null,
            IReadOnlyDictionary<string, object> systemProperties = null,
            string partitionId = null)
        {
            Id = id;
            Content = content ?? new byte[0];
            Properties = properties ?? new Dictionary<string, object>();
            SystemProperties = systemProperties ?? new Dictionary<string, object>();
            PartitionId = partitionId;
        }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the message content as byte array.
        /// </summary>
        public byte[] Content { get; }

        /// <summary>
        /// Gets the partition key.
        /// </summary>
        public string PartitionId { get; }

        /// <summary>
        /// Gets the properties for this message.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the internal properties for this message.
        /// </summary>
        public IReadOnlyDictionary<string, object> SystemProperties { get; }

        /// <summary>
        /// Clones the message.
        /// </summary>
        /// <returns>The cloned message.</returns>
        public Message Clone()
        {
            return new Message(Id, Content, Properties, SystemProperties, PartitionId);
        }
    }
}
