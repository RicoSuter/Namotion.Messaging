using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Amazon.SQS
{
    /// <summary>
    /// An Amazon SQS message publisher.
    /// </summary>
    public class AmazonSqsMessagePublisher : IMessagePublisher
    {
        private readonly AmazonSQSClient _client;
        private readonly string _queueName;

        private string _queueUrl;

        private AmazonSqsMessagePublisher(AmazonSQSClient client, string queueName)
        {
            _client = client;
            _queueName = queueName;
        }

        /// <summary>
        /// Creates a new Amazon SQS message receiver.
        /// </summary>
        /// <param name="client">The Amazon SQS client.</param>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The publisher.</returns>
        public static IMessagePublisher Create(AmazonSQSClient client, string queueName)
        {
            return new AmazonSqsMessagePublisher(client, queueName);
        }

        /// <inheritdoc/>
        public async Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            var batch = new SendMessageBatchRequest
            {
                QueueUrl = await GetQueueUrl().ConfigureAwait(false),
                Entries = messages.Select(m => new SendMessageBatchRequestEntry
                {
                    Id = m.Id ?? Guid.NewGuid().ToString(),
                    MessageBody = Convert.ToBase64String(m.Content),
                    MessageGroupId = m.PartitionId,
                    MessageAttributes = m.Properties.ToDictionary(p => p.Key, p => new MessageAttributeValue
                    {
                        StringValue = p.Value.ToString(),
                        DataType = "String"
                    }),
                }).ToList()
            };

            var response = await _client.SendMessageBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            if (response.Failed.Any())
            {
                throw new AmazonSQSException("Not all messages have been sent.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.Dispose();
        }

        private async Task<string> GetQueueUrl()
        {
            if (_queueUrl == null)
            {
                var response = await _client.GetQueueUrlAsync(_queueName);
                _queueUrl = response.QueueUrl;
            }

            return _queueUrl;
        }
    }
}
