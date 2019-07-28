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
    public static class IMessageReceiverExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static Task ListenJsonAsync<T>(
            this IMessageReceiver<T> messageReceiver,
            Func<IReadOnlyCollection<Message<T>>, CancellationToken, Task> handleMessages,
            CancellationToken cancellationToken = default)
        {
            return messageReceiver.ListenAsync((messages, ct) => handleMessages(messages.Select(ConvertFromMessage<T>).ToArray(), ct), cancellationToken);
        }

        private static Message<T> ConvertFromMessage<T>(Message message)
        {
            var json = Encoding.UTF8.GetString(message.Content);
            var deserializedObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
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
