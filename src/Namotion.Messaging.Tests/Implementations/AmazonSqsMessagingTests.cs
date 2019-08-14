using Namotion.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Namotion.Messaging.Amazon.SQS;
using Amazon.SQS;
using Amazon;

namespace Namotion.Messaging.Tests.Implementations
{
    public class AmazonSqsMessagingTests : MessagingTestsBase
    {
        protected override IMessageReceiver<MyMessage> CreateMessageReceiver(IConfiguration configuration)
        {
            return AmazonSqsMessageReceiver
                .Create(new AmazonSQSClient(configuration["AwsAccessKeyId"], configuration["AwsAccessKey"], RegionEndpoint.EUCentral1), "namotionqueue")
                .WithMessageType<MyMessage>();
        }

        protected override IMessagePublisher<MyMessage> CreateMessagePublisher(IConfiguration configuration)
        {
            return AmazonSqsMessagePublisher
                .Create(new AmazonSQSClient(configuration["AwsAccessKeyId"], configuration["AwsAccessAccessKey"], RegionEndpoint.EUCentral1), "namotionqueue")
                .WithMessageType<MyMessage>();
        }
    }
}
