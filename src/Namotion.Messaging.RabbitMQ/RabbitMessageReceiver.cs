using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Namotion.Messaging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Namotion.Messaging.RabbitMQ
{
    public class RabbitMessageReceiver : IMessageReceiver
    {
        private const string DeliveryTagProperty = "DeliveryTag";

        private readonly RabbitConfiguration _configuration;
        private readonly IMessagePublisher _deadLetterPublisher;

        private IModel _channel;

        public RabbitMessageReceiver(RabbitConfiguration configuration, IMessagePublisher deadLetterPublisher = null)
        {
            _configuration = configuration;
            _deadLetterPublisher = deadLetterPublisher;
        }

        public async Task ListenAsync(Func<IEnumerable<QueueMessage>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
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
                    var message = new QueueMessage(a.Body)
                    {
                        Id = a.BasicProperties.MessageId,
                        SystemProperties =
                        {
                            { DeliveryTagProperty, a.DeliveryTag }
                        }
                    };

                    await handleMessages(new QueueMessage[] { message }, cancellationToken);
                };

                _channel.BasicConsume(_configuration.QueueName, _configuration.AutoAck, consumer);
                await Task.Delay(Timeout.Infinite, cancellationToken);
                _channel = null;
            }
        }

        public Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<long>(_channel.MessageCount(_configuration.QueueName));
        }

        public Task ConfirmAsync(IEnumerable<QueueMessage> messages, CancellationToken cancellationToken = default)
        {
            _ = _channel ?? throw new InvalidOperationException("Queue is not in listening mode.");

            foreach (var message in messages)
            {
                _channel.BasicAck((ulong)message.SystemProperties[DeliveryTagProperty], true);
            }

            return Task.CompletedTask;
        }

        public Task RejectAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            _ = _channel ?? throw new InvalidOperationException("Queue is not in listening mode.");

            _channel.BasicReject((ulong)message.Properties[DeliveryTagProperty], true);
            return Task.CompletedTask;
        }

        public async Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            _ = _deadLetterPublisher ?? throw new InvalidOperationException("Dead letter publisher not specified.");

            await _deadLetterPublisher.SendAsync(new QueueMessage[] { message }, cancellationToken).ConfigureAwait(false);
        }

        public Task KeepAliveAsync(QueueMessage message, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}