using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Namotion.Messaging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging
{
    /// <summary>
    /// <see cref="IMessageReceiver"/> extension methods.
    /// </summary>
    public static class NewtonsoftJsonMessageReceiverExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Receives messages, deserializes the JSON in the content and passes the result to the <paramref name="handleMessages"/> callback.
        /// The task completes when the listener throws an exception or the <paramref name="cancellationToken"/> is cancelled.
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
            return ListenAndDeserializeJsonAsync(messageReceiver, handleMessages, NullLogger.Instance, cancellationToken);
        }

        /// <summary>
        /// Receives messages, deserializes the JSON in the content and passes the result to the <paramref name="handleMessages"/> callback.
        /// The task completes when the listener throws an exception or the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="handleMessages">The message handler callback.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ListenAndDeserializeJsonAsync<T>(
            this IMessageReceiver<T> messageReceiver,
            Func<IReadOnlyCollection<Message<T>>, CancellationToken, Task> handleMessages,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            return messageReceiver.ListenAsync((messages, ct) =>
                handleMessages(messages.Select(m => ConvertFromMessage<T>(m, logger)).ToArray(), ct), cancellationToken);
        }

        /// <summary>
        /// Receives messages, deserializes the JSON in the content and passes the result to the <paramref name="handleMessages"/> callback.
        /// The task completes when the listener throws an exception or the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="handleMessages">The message handler callback.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task ListenWithRetryAndDeserializeJsonAsync<T>(
            this IMessageReceiver<T> messageReceiver,
            Func<IReadOnlyCollection<Message<T>>, CancellationToken, Task> handleMessages,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            return messageReceiver.ListenWithRetryAsync((messages, ct) =>
                handleMessages(messages.Select(m => ConvertFromMessage<T>(m, logger)).ToArray(), ct), logger, cancellationToken);
        }

        private static Message<T> ConvertFromMessage<T>(Message message, ILogger logger)
        {
            T deserializedObject;
            try
            {
                var json = Encoding.UTF8.GetString(message.Content);
                deserializedObject = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            }
            catch (Exception e)
            {
                logger.LogError(new EventId(), e, "Failed to deserialize message JSON.");
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
