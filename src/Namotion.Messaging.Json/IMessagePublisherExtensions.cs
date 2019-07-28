using Namotion.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Json
{
    public static class IMessagePublisherExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static Task SendJsonAsync<T>(this IMessagePublisher<T> messagePublisher, T message, CancellationToken cancellationToken = default)
        {
            return messagePublisher.SendAsync(ConvertToMessage(message), cancellationToken);
        }

        public static Task SendJsonAsync<T>(this IMessagePublisher<T> messagePublisher, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        {
            return messagePublisher.SendAsync(messages.Select(ConvertToMessage), cancellationToken);
        }

        private static Message ConvertToMessage<T>(T message)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));
            return new Message(bytes);
        }
    }
}
