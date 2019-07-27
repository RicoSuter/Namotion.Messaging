# Namotion.Messaging

## Packages

**Namotion.Messaging.Abstractions**

Contains the interfaces for working with various messaging implementations:

- IMessagePublisher
- IMessageReceiver

**Namotion.Messaging**

Contains common helper methods and technology independent implementations for the abstractions:

- **InMemoryMessagePublisherReceiver:** In-memory message publisher and receiver for integration tests and dependency free local development environments.

**Namotion.Messaging.Azure.EventHub**

Implementations:

- EventHubMessagePublisher
- EventHubMessageReceiver

Dependencies: 

- Microsoft.Azure.EventHubs.Processor

**Namotion.Messaging.Azure.ServiceBus**

Implementations:

- ServiceBusMessagePublisher
- ServiceBusMessageReceiver

Dependencies: 

- Microsoft.Azure.ServiceBus

**Namotion.Messaging.RabbitMQ**

Implementations:

- RabbitMessagePublisher
- RabbitMessageReceiver

Dependencies: 

- RabbitMQ.Client