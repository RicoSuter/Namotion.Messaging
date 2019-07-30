using Namotion.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Json
{
    /// <summary>
    /// <see cref="IMessageReceiver"/> extension methods.
    /// </summary>
    public static class IMessageReceiverExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Receives messages, deserializes the JSON in the content and passes the result to the <paramref name="handleMessages"/> callback.
        /// The task does not complete until the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="handleMessages">The message handler callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ListenAndDeserializeJsonAsync<T>(
            this IMessageReceiver<T> messageReceiver,
            Func<IReadOnlyCollection<Message<T>>, CancellationToken, Task> handleMessages,
            CancellationToken cancellationToken = default)
        {
            return messageReceiver.ListenAsync((messages, ct) => handleMessages(messages.Select(ConvertFromMessage<T>).ToArray(), ct), cancellationToken);
        }

        private static Message<T> ConvertFromMessage<T>(Message message)
        {
            T deserializedObject;
            try
            {
                var json = Encoding.UTF8.GetString(message.Content);
                deserializedObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            }
            catch
            {
                // TODO: What to do here?
                deserializedObject = default;
            }

            return new Message<T>(
                message.Id,
                message.Content,
                deserializedObject,
                message.Properties,
                message.SystemProperties,
                message.PartitionId);
        }
    }
}
