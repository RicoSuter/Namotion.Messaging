using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Messaging.Amazon.SQS
{
    /// <summary>
    /// An Amazon SQS message receiver.
    /// </summary>
    public class AmazonSqsMessageReceiver : IMessageReceiver
    {
        private static readonly TimeSpan RetryAfterException = TimeSpan.FromSeconds(10);
        private static readonly int WaitTimeSeconds = 20;

        private const string ReceiptHandleProperty = "ReceiptHandle";
        private const string NativeMessageProperty = "NativeMessage";

        private readonly AmazonSQSClient _client;
        private readonly string _queueName;
        private string _queueUrl;
        private int _maxNumberOfMessages;

        private AmazonSqsMessageReceiver(AmazonSQSClient client, string queueName, int maxNumberOfMessages = 1)
        {
            _client = client;
            _queueName = queueName;
            _maxNumberOfMessages = maxNumberOfMessages;
        }

        /// <summary>
        /// Creates a new Amazon SQS message receiver.
        /// </summary>
        /// <param name="client">The Amazon SQS client.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="maxNumberOfMessages">The maximum number of messages per batch (default: 1).</param>
        /// <returns>The receiver.</returns>
        public static IMessageReceiver Create(AmazonSQSClient client, string queueName, int maxNumberOfMessages = 1)
        {
            return new AmazonSqsMessageReceiver(client, queueName, maxNumberOfMessages);
        }

        /// <inheritdoc/>
        public async Task ListenAsync(Func<IReadOnlyCollection<Message>, CancellationToken, Task> handleMessages, CancellationToken cancellationToken = default)
        {
            _ = handleMessages ?? throw new ArgumentNullException(nameof(handleMessages));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = await GetQueueUrl().ConfigureAwait(false),
                        MessageAttributeNames = new List<string> { ".*" },
                        WaitTimeSeconds = WaitTimeSeconds,
                        MaxNumberOfMessages = _maxNumberOfMessages
                    };

                    var response = await _client.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);
                    if (response.Messages != null && response.Messages.Any())
                    {
                        var messages = response.Messages.Select(m => ConvertToMessage(m)).ToArray();
                        try
                        {
                            await handleMessages(messages, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            await RejectAsync(messages, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        await Task.Delay(RetryAfterException, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await Task.Delay(RetryAfterException, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public async Task ConfirmAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            var response = await _client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
            {
                QueueUrl = await GetQueueUrl().ConfigureAwait(false),
                Entries = messages.Select(m => new DeleteMessageBatchRequestEntry
                {
                    Id = m.Id,
                    ReceiptHandle = (string)m.SystemProperties[ReceiptHandleProperty]
                }).ToList()
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeadLetterAsync(IEnumerable<Message> messages, string reason, string errorDescription, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public async Task<long> GetMessageCountAsync(CancellationToken cancellationToken = default)
        {
            var request = new GetQueueAttributesRequest
            {
                QueueUrl = await GetQueueUrl().ConfigureAwait(false)
            };

            var response = await _client.GetQueueAttributesAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ApproximateNumberOfMessages;
        }

        /// <inheritdoc/>
        public async Task KeepAliveAsync(IEnumerable<Message> messages, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            var response = await _client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
            {
                QueueUrl = await GetQueueUrl().ConfigureAwait(false),
                Entries = messages.Select(m => new ChangeMessageVisibilityBatchRequestEntry
                {
                    Id = m.Id,
                    ReceiptHandle = (string)m.SystemProperties[ReceiptHandleProperty],
                    VisibilityTimeout = timeToLive.HasValue ? (int)timeToLive.Value.TotalSeconds : 60 * 3
                }).ToList()
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RejectAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            var response = await _client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
            {
                QueueUrl = await GetQueueUrl().ConfigureAwait(false),
                Entries = messages.Select(m => new ChangeMessageVisibilityBatchRequestEntry
                {
                    Id = m.Id,
                    ReceiptHandle = (string)m.SystemProperties[ReceiptHandleProperty],
                    VisibilityTimeout = 0
                }).ToList()
            }, cancellationToken).ConfigureAwait(false);
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

        private Message ConvertToMessage(global::Amazon.SQS.Model.Message message)
        {
            return new Message(
                id: message.MessageId,
                content: Convert.FromBase64String(message.Body),
                properties: message.MessageAttributes.ToDictionary(a => a.Key, a => (object)a.Value.StringValue),
                systemProperties: new Dictionary<string, object>
                {
                    { ReceiptHandleProperty, message.ReceiptHandle },
                    { NativeMessageProperty, message },
                });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
#pragma warning disable CS1998
        public async ValueTask DisposeAsync()
        {
        }
    }
}
