# Namotion.Messaging

[![Azure DevOps](https://img.shields.io/azure-devops/build/rsuter/Namotion/19/master.svg)](https://rsuter.visualstudio.com/Namotion/_build?definitionId=19)

The Namotion.Messaging .NET libraries provide abstractions and implementations for message/event queues and data ingestion services.

This enables the following scenarios: 

- Build **multi-cloud capable applications** by easily change messaging technologies on demand.
- Quickly **try out different messaging technologies** to find the best fit for your applications.
- Implement behavior driven integration **tests which can run in-memory** or against different technologies for debugging and faster execution. 
- Provide **better local development experiences** (e.g. replace Service Bus with the dockerizable RabbitMQ technology locally).

## Extensibility

Extensibility, for example custom dead letter queues or large message handling, is achieved with interceptors which wrap publisher and receiver methods with custom code. These interceptors are added with the `With*` extension methods described below. Custom interceptors can easily implemented with the `MessagePublisher<T>` and `MessageReceiver<T>` classes.

## Usage

To use the `IMessageReceiver` in a simple command line application, implement a new `BackgroundService` and start message processing in `ExecuteAsync`:

```CSharp
public class MyBackgroundService : BackgroundService
{
    private IMessageReceiver _messageReceiver;

    public MyBackgroundService(IMessageReceiver messageReceiver)
    {
        _messageReceiver = messageReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _messageReceiver.ListenAsync(stoppingToken);
    }
}
```

In your program's `Main` method, create a new `HostBuilder` and add the background service as a hosted service:

```CSharp
public static async Task Main(string[] args)
{
    var host = new HostBuilder()
        .ConfigureServices(services => 
        {
            var receiver = ServiceBusMessageReceiver.Create("MyConnectionString", "myqueue");
            services.AddSingleton<IMessageReceiver>(receiver);
            services.AddHostedService<MyBackgroundService>();
        })
        .Build();

    await host.RunAsync();
}
```

## Core packages

### Namotion.Messaging.Abstractions

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Abstractions.svg)](https://www.nuget.org/packages/Namotion.Messaging.Abstractions/)

Contains the messaging abstractions, mainly interfaces with a very small footprint and extremely stable contracts:

- **IMessagePublisher\<T>**
- **IMessagePublisher**
    - `SendAsync()`: Sends a batch of messages to the queue.
- **IMessageReceiver\<T>**
- **IMessageReceiver**
    - `GetMessageCountAsync()`: Gets the count of messages waiting to be processed.
    - `ListenAsync(handleMessages, cancellationToken)`: Starts listening and processing messages with the `handleMessages` function until the `cancellationToken` signals a cancellation.
    - `KeepAliveAsync()`: Extends the message lock timeout on the given message.
    - `ConfirmAsync()`: Confirms the processing of messages and removes them from the queue.
    - `RejectAsync()`: Rejects a message and requeues it for later reprocessing.
    - `DeadLetterAsync()`: Removes the message and moves it to the dead letter queue.
- **Message\<T>:**
- **Message:** A generic message implementation.

### Namotion.Messaging.Json

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Json.svg)](https://www.nuget.org/packages/Namotion.Messaging.Json/)

New extension methods on `IMessagePublisher<T>` and `IMessageReceiver<T>`: 

- **SendJsonAsync(...):** Sends messages of type T which are serialized to JSON to the queue.
- **ListenJsonAsync(...):** Receives messages and deserializes their content using the JSON serializer to the `Message<T>.Object` property. If the content could not be deserialized then `Object` is `null`.

Send a JSON encoded message: 

```CSharp
var publisher = ServiceBusMessagePublisher
    .Create("MyConnectionString", "myqueue")
    .WithMessageType<OrderCreatedMessage>();

await publisher.SendJsonAsync(new OrderCreatedMessage { ... });
```

Receive JSON encoded messages:

```CSharp
var publisher = ServiceBusMessageReceiver
    .Create("MyConnectionString", "myqueue")
    .WithMessageType<OrderCreatedMessage>();

await publisher.ListenJsonAsync(async (messages, ct) => 
{
    foreach (OrderCreatedMessage message in messages.Select(m => m.Object))
    {
        ...
    }

    await publisher.ConfirmAsync(messages, ct);
});
```

## Implementation packages

The following packages should only be used in the head, i.e. directly in your application bootstrapping project where the dependency injection container is initialized.

|                       | ServiceBus     | EventHub            | RabbitMQ            | InMemory   |
|-----------------------|----------------|---------------------|---------------------|------------|
| SendAsync             | Supported      | Supported           | Supported           | Supported  |
| ListenAsync           | Supported      | Supported           | Supported           | Supported  |
| GetMessageCountAsync  | Not supported  | Not supported       | Supported           | Supported  |
| KeepAliveAsync        | Supported      | Ignored (1.)        | Not supported       | Ignored    |
| ConfirmAsync          | Supported      | Ignored (1.)        | Supported           | Ignored    |
| RejectAsync           | Supported      | Ignored (1.)        | Supported           | Supported  |
| DeadLetterAsync       | Supported      | Not supported (2.)  | Not supported (2.)  | Supported  |
| User properties       | Supported      | Supported           | Supported           | Supported  |

1) Because Event Hub is stream based and transactional, these method calls are just ignored.
2) Use `receiver.WithDeadLettering(publisher)` to enable dead letter support.

### Namotion.Messaging

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.svg)](https://www.nuget.org/packages/Namotion.Messaging/)

Contains common helper methods and technology independent implementations for the abstractions:

- **InMemoryMessagePublisherReceiver:** In-memory message publisher and receiver for integration tests and dependency free local development environments (i.e. use this implementation when no connection strings are defined).

Extension methods to enhance or modify instances: 

- **WithMessageType\<T>():** Changes the type of the interface from `IMessagePublisher`/`IMessageReceiver` to `IMessagePublisher<T>`/`IMessageReceiver<T>`.
- **WithDeadLettering(messagePublisher):** Adds support for a custom dead letter queue, i.e. a call to `DeadLetterAsync()` will confirm the message and publish it to the specified `messagePublisher`.

### Namotion.Messaging.Azure.ServiceBus

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Azure.ServiceBus.svg)](https://www.nuget.org/packages/Namotion.Messaging.Azure.ServiceBus/)

Implementations:

- **ServiceBusMessagePublisher**
- **ServiceBusMessageReceiver**

Behavior: 

- When `handleMessages` throws an exception, then the message is abandoned and later reprocessed until it is moved to the dead letter queue.

Dependencies: 

- [Microsoft.Azure.ServiceBus](https://www.nuget.org/packages/Microsoft.Azure.ServiceBus/)

### Namotion.Messaging.Azure.EventHub

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Azure.EventHub.svg)](https://www.nuget.org/packages/Namotion.Messaging.Azure.EventHub/)

Implementations:

- **EventHubMessagePublisher**
- **EventHubMessageReceiver**

Behavior: 

- Exceptions from `handleMessages` are logged and then ignored, i.e. the processing moves forward in the partition.

Dependencies: 

- [Microsoft.Azure.EventHubs.Processor](https://www.nuget.org/packages/Microsoft.Azure.EventHubs.Processor/)

### Namotion.Messaging.RabbitMQ

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.RabbitMQ.svg)](https://www.nuget.org/packages/Namotion.Messaging.RabbitMQ/)

Implementations:

- **RabbitMessagePublisher**
- **RabbitMessageReceiver**

Behavior: 

- When `handleMessages` throws an exception, then the message is rejected and later reprocessed.

Dependencies: 

- [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client)
