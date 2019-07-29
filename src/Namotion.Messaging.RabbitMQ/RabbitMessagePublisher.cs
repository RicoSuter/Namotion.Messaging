using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Namotion.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Namotion.Messaging.RabbitMQ
{
    /// <summary>
    /// A RabbitMQ message publisher.
    /// </summary>
    public class RabbitMessagePublisher : IMessagePublisher
    {
        private readonly object _lock = new object();
        private readonly RabbitConfiguration _configuration;

        private IConnection _connection;
        private IModel _channel;

        private RabbitMessagePublisher(RabbitConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a new RabbitMQ message publisher.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The message publisher.</returns>
        public static IMessagePublisher Create(RabbitConfiguration configuration)
        {
            return new RabbitMessagePublisher(configuration);
        }

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                lock (_lock)
                {
                    if (_connection == null)
                    {
                        var factory = new ConnectionFactory
                        {
                            HostName = _configuration.Host,
                            UserName = _configuration.Username,
                            Password = _configuration.Password,
                        };

                        _connection = factory.CreateConnection();

                        _channel = _connection.CreateModel();
                        _channel.ExchangeDeclare(_configuration.ExchangeName, ExchangeType.Direct);
                        _channel.QueueDeclare(_configuration.QueueName, true, false, false, null);
                        _channel.QueueBind(_configuration.QueueName, _configuration.ExchangeName, _configuration.Routingkey, null);
                    }
                }
            }

            foreach (var message in messages)
            {
                _channel.BasicPublish(
                    exchange: _configuration.ExchangeName,
                    routingKey: _configuration.Routingkey,
                    basicProperties: null,
                    body: message.Content);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}