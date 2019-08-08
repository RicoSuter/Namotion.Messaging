using System;
using System.Collections.Generic;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// A generic message with deserialized content to be used in all queue implementations.
    /// </summary>
    public class Message<T> : Message, IEquatable<Message<T>>
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

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Message<T>);
        }

        /// <inheritdoc/>
        public bool Equals(Message<T> other)
        {
            return other != null &&
                   base.Equals(other) &&
                   EqualityComparer<T>.Default.Equals(Object, other.Object);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 180369264;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Object);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(Message<T> left, Message<T> right)
        {
            return EqualityComparer<Message<T>>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Message<T> left, Message<T> right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// A generic message to be used in all queue implementations.
    /// </summary>
    public class Message : IEquatable<Message>
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

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Message);
        }

        /// <inheritdoc/>
        public bool Equals(Message other)
        {
            return other != null &&
                   Id == other.Id &&
                   EqualityComparer<byte[]>.Default.Equals(Content, other.Content) &&
                   PartitionId == other.PartitionId &&
                   EqualityComparer<IReadOnlyDictionary<string, object>>.Default.Equals(Properties, other.Properties) &&
                   EqualityComparer<IReadOnlyDictionary<string, object>>.Default.Equals(SystemProperties, other.SystemProperties);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -268984773;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Content);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PartitionId);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyDictionary<string, object>>.Default.GetHashCode(Properties);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyDictionary<string, object>>.Default.GetHashCode(SystemProperties);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(Message left, Message right)
        {
            return EqualityComparer<Message>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Message left, Message right)
        {
            return !(left == right);
        }
    }
}
