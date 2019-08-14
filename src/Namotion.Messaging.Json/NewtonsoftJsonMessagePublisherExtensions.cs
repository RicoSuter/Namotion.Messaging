using Namotion.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// JSON serialization extension methods.
    /// </summary>
    public static class NewtonsoftJsonMessagePublisherExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Serializes the message to JSON and sends a single message to the queue.
        /// </summary>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task PublishAsJsonAsync<T>(this IMessagePublisher<T> messagePublisher, T message, CancellationToken cancellationToken = default)
        {
            return messagePublisher.PublishAsync(ConvertToMessage(message), cancellationToken);
        }

        /// <summary>
        /// Serializes the messages to JSON and sends them to the queue.
        /// </summary>
        /// <param name="messagePublisher">The message publisher.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task PublishAsJsonAsync<T>(this IMessagePublisher<T> messagePublisher, IEnumerable<T> messages, CancellationToken cancellationToken = default)
        {
            return messagePublisher.PublishAsync(messages.Select(ConvertToMessage), cancellationToken);
        }

        private static Message ConvertToMessage<T>(T message)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));
            return new Message(bytes);
        }
    }
}
