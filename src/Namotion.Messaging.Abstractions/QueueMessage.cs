using System.Collections.Generic;

namespace Namotion.Messaging.Abstractions
{
    /// <summary>
    /// Message to be used in all the queueing code
    /// </summary>
    public class QueueMessage
    {
        /// <summary>
        /// Creates an instance of <see cref="QueueMessage"/>
        /// </summary>
        /// <param name="content">The message content.</param>
        public QueueMessage(byte[] content)
        {
            Content = content;
        }

        /// <summary>
        /// Gets or sets the message ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the message content as byte array.
        /// </summary>
        public byte[] Content { get; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        public string PartitionId { get; set; }

        /// <summary>
        /// Gets or sets the count of how many time this message was dequeued
        /// </summary>
        public int DequeueCount { get; set; }

        /// <summary>
        /// Gets or sets the properties for this message.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the internal properties for this message.
        /// </summary>
        public Dictionary<string, object> SystemProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Clones the message.
        /// </summary>
        /// <returns>The cloned message.</returns>
        public QueueMessage Clone()
        {
            return new QueueMessage(Content)
            {
                Id = Id,
                PartitionId = PartitionId,
                DequeueCount = DequeueCount,
                Properties = new Dictionary<string, object>(Properties)
            };
        }
    }
}
