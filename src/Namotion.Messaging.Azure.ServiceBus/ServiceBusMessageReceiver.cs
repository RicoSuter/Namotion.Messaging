using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Namotion.Messaging.Azure.ServiceBus
{
    /// <summary>
    /// A Service Bus message receiver.
    /// </summary>
    public class ServiceBusMessageReceiver : IMessageReceiver, IAsyncDisposable
    {
        private const string LockTokenProperty = "LockToken";
        private const string DeliveryCountProperty = "DeliveryCount";

        private ServiceBusClient _client;
        private ServiceBusReceiver _receiver;

        private readonly string _queueName;
        private readonly int _maxBatchSize;
        private readonly bool _disposeClient;

        private ServiceBusMessageReceiver(ServiceBusClient serviceBusClient, string queueName, int maxBatchSize, bool disposeClient = false)
        {
            _client = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
            _queueName = queueName;
            _maxBatchSize = maxBatchSize;
            _disposeClient = disposeClient;
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a client.
        /// </summary>
        /// <param name="serviceBusClient">The service bus client.</param>
        /// <param name="queueName">The queue name to listen on.</param>
        /// <param name="maxMessageCount">The maximum message count (default: 1).</param>
        /// <param name="disposeClient">Specifies whether to dispose the client when this receiver is disposed.</param>
        /// <returns>The message publisher.</returns>
        public static IMessageReceiver CreateFromServiceBusClient(ServiceBusClient serviceBusClient, string queueName, int maxMessageCount = 1, bool disposeClient = false)
        {
            return new ServiceBusMessageReceiver(serviceBusClient, queueName, maxMessageCount, disposeClient);
        }

        /// <summary>
        /// Creates a new Service Bus receiver from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="queueName">The entity path.</param>
        /// <param name="maxBatchSize">The maximum batch size (default: 1).</param>
        /// <returns>The message publisher.</returns>
        public static IMessageReceiver Create(
            string connectionString, string queueName, int maxBatchSize = 1)
        {
            var client = new ServiceBusClient(connectionString);
            return new ServiceBusMessageReceiver(client, queueName, maxBatchSize, true);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            if (_receiver != null)
            {
                throw new InvalidOperationException("Receiver is already listening.");
            }

            try
            {
                _receiver = _client.CreateReceiver(_queueName);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var messages = await _receiver
                        .ReceiveMessagesAsync(_maxBatchSize, TimeSpan.FromSeconds(30), cancellationToken)
                        .ConfigureAwait(false);

                    if (messages != null && messages.Any())
                    {
                        var abstractMessages = messages.Select(ConvertToMessage).ToArray();
                        try
                        {
                            await handleMessages(abstractMessages, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            await RejectAsync(abstractMessages, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                await _receiver.DisposeAsync().ConfigureAwait(false);
                _receiver = null;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                return _receiver.RenewMessageLockAsync((ServiceBusReceivedMessage)m.SystemProperties[nameof(ServiceBusReceivedMessage)], cancellationToken);
            }));
        }

        /// <inheritdoc/>
        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                return _receiver.CompleteMessageAsync((ServiceBusReceivedMessage)m.SystemProperties[nameof(ServiceBusReceivedMessage)], cancellationToken);
            }));
        }

        /// <inheritdoc/>
        public Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                return _receiver.AbandonMessageAsync((ServiceBusReceivedMessage)m.SystemProperties[nameof(ServiceBusReceivedMessage)], null, cancellationToken);
            }));
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            return Task.WhenAll(messages.Select(m =>
            {
                return _receiver.DeadLetterMessageAsync((ServiceBusReceivedMessage)m.SystemProperties[nameof(ServiceBusReceivedMessage)], reason, errorDescription, cancellationToken);
            }));
        }

        private Message ConvertToMessage(ServiceBusReceivedMessage message)
        {
            return new Message(
                id: message.MessageId,
                content: message.Body.ToArray(),
                properties: message.ApplicationProperties.ToDictionary(t => t.Key, t => t.Value),
                partitionId: message.SessionId,
                systemProperties: new Dictionary<string, object>
                {
                    { LockTokenProperty, message.LockToken },
                    { DeliveryCountProperty, message.DeliveryCount },
                    { nameof(ServiceBusReceivedMessage), message },
                });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposeClient)
            {
                await _client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}