namespace Namotion.Messaging.RabbitMQ
{
    public class RabbitConfiguration
    {
        public string Host { get; set; }

        public string Username { get; set; } = "rabbitmq";

        public string Password { get; set; } = "rabbitmq";

        public string QueueName { get; set; } = "test_queue";

        public string ExchangeName { get; set; } = "test_exchange";

        public string Routingkey { get; set; } = "test_routing_key";

        public bool AutoAck { get; set; } = false;
    }
}