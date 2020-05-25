using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Namotion.Messaging.RabbitMQ
{
    /// <summary>
    /// A RabbitMQ message receiver.
    /// </summary>
    public class RabbitMessageReceiver : IMessageReceiver
    {
        private const string DeliveryTagProperty = "DeliveryTag";

        private readonly RabbitConfiguration _configuration;

        private IModel _channel;

        private RabbitMessageReceiver(RabbitConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a new RabbitMQ message receiver.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The message receiver.</returns>
        public static IMessageReceiver Create(RabbitConfiguration configuration)
        {
            return new RabbitMessageReceiver(configuration);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));
            if (_channel != null)
            {
                throw new InvalidOperationException("The receiver is already listening for messages.");
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration.Host,
                UserName = _configuration.Username,
                Password = _configuration.Password,
                DispatchConsumersAsync = true
            };

            using (var connection = factory.CreateConnection())
            using (_channel = connection.CreateModel())
            {
                _channel.ExchangeDeclare(_configuration.ExchangeName, ExchangeType.Direct);
                _channel.QueueDeclare(_configuration.QueueName, true, false, false, null);
                _channel.QueueBind(_configuration.QueueName, _configuration.ExchangeName, _configuration.Routingkey, null);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (o, a) =>
                {
                    var message = new Message(
                        id: a.BasicProperties.MessageId,
                        content: a.Body.ToArray(),
                        systemProperties: new Dictionary<string, object>
                        {
                            { DeliveryTagProperty, a.DeliveryTag }
                        }
                    );

                    var messages = new Message[] { message };
                    try
                    {
                        await handleMessages(messages, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        await RejectAsync(messages, cancellationToken).ConfigureAwait(false);
                    }
                };

                _channel.BasicConsume(_configuration.QueueName, _configuration.AutoAck, consumer);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            _channel = null;
        }

        /// <inheritdoc/>
        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<long>(_channel.MessageCount(_configuration.QueueName));
        }

        /// <inheritdoc/>
        public Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            _ = _channel ?? throw new InvalidOperationException("Queue is not in listening mode.");

            foreach (var message in messages)
            {
                _channel.BasicAck((ulong)message.SystemProperties[DeliveryTagProperty], true);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            _ = _channel ?? throw new InvalidOperationException("Queue is not in listening mode.");

            foreach (var message in messages)
            {
                _channel.BasicReject((ulong)message.Properties[DeliveryTagProperty], true);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException" />
        public Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}