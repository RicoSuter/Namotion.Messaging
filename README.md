# Namotion.Messaging

[![Azure DevOps](https://img.shields.io/azure-devops/build/rsuter/Namotion/19/master.svg)](https://dev.azure.com/rsuter/Namotion/_build?definitionId=19)

<img align="left" src="https://raw.githubusercontent.com/RicoSuter/Namotion.Reflection/master/assets/Icon.png" width="48px" height="48px">

The Namotion.Messaging .NET libraries provide abstractions and implementations for message brokers, event queues and data ingestion services.

By programming against a messaging abstraction you enable the following scenarios: 

- Build **multi-cloud capable applications** by being able to change messaging technologies on demand. 
- Quickly **switch to different messaging technologies** to find the best technological fit for your applications. 
- Implement behavior driven **integration tests which can run in-memory** or against different technologies for better debugging experiences or local execution. 
- Provide **better local development experiences**, e.g. replace Service Bus with a locally running RabbitMQ docker container or an in-memory implementation. 

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
        await _messageReceiver.ListenAsync(ProcessMessagesAsync, stoppingToken);
    }

    private async Task ProcessMessagesAsync(IReadOnlyCollection<Message> messages, CancellationToken cancellationToken)
    {
        ...

        await _messageReceiver.ConfirmAsync(messages, cancellationToken);
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

## Extensions

Behavior extensions, for example custom dead letter queues or large message handling, is achieved with interceptors which wrap publisher and receiver methods with custom code. These interceptors are added with the `With*` extension methods. Custom interceptors can be implemented with the `MessagePublisher<T>` and `MessageReceiver<T>` classes.

## Core packages

### Namotion.Messaging.Abstractions

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Abstractions.svg)](https://www.nuget.org/packages/Namotion.Messaging.Abstractions/)

Contains the messaging abstractions, mainly interfaces with a very small footprint and extremely stable contracts:

- **IMessagePublisher\<T>**
- **IMessagePublisher**
    - `SendAsync(messages, cancellationToken)`: Sends a batch of messages to the queue.
- **IMessageReceiver\<T>**
- **IMessageReceiver**
    - `GetMessageCountAsync(cancellationToken)`: Gets the count of messages waiting to be processed.
    - `ListenAsync(handleMessages, cancellationToken)`: Starts listening and processing messages with the `handleMessages` function until the `cancellationToken` signals a cancellation.
    - `KeepAliveAsync(messages, timeToLive, cancellationToken)`: Extends the message lock timeout on the given messages.
    - `ConfirmAsync(messages, cancellationToken)`: Confirms the processing of messages and removes them from the queue.
    - `RejectAsync(messages, cancellationToken)`: Rejects messages and requeues them for later reprocessing.
    - `DeadLetterAsync(messages, reason, errorDescription, cancellationToken)`: Removes the messages and moves them to the dead letter queue.
- **Message\<T>:**
- **Message:** A generic message implementation.

The idea behind the generic interfaces is to allow multiple instance registrations, read [Dependency Injection in .NET: A way to work around missing named registrations](https://blog.rsuter.com/dotnet-dependency-injection-way-to-work-around-missing-named-registrations/) for more information.

### Namotion.Messaging.Json

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Json.svg)](https://www.nuget.org/packages/Namotion.Messaging.Json/)

Provides extension methods on `IMessagePublisher<T>` and `IMessageReceiver<T>` to enable JSON serialization for messages: 

- **SendAsJsonAsync(...):** Sends messages of type T which are serialized to JSON to the queue.
- **ListenAndDeserializeJsonAsync(...):** Receives messages and deserializes their content using the JSON serializer to the `Message<T>.Object` property. If the content could not be deserialized then `Object` is `null`.

Send a JSON encoded message: 

```CSharp
var publisher = ServiceBusMessagePublisher
    .Create("MyConnectionString", "myqueue")
    .WithMessageType<OrderCreatedMessage>();

await publisher.SendAsJsonAsync(new OrderCreatedMessage { ... });
```

Receive JSON encoded messages:

```CSharp
var publisher = ServiceBusMessageReceiver
    .Create("MyConnectionString", "myqueue")
    .WithMessageType<OrderCreatedMessage>();

await publisher.ListenAndDeserializeJsonAsync(async (messages, ct) => 
{
    foreach (OrderCreatedMessage message in messages.Select(m => m.Object))
    {
        ...
    }

    await publisher.ConfirmAsync(messages, ct);
});
```

## Implementation packages

The following packages should only be used in the head project, i.e. directly in your application bootstrapping project where the dependency injection container is initialized.

|                       | Azure<br /> Service Bus | Azure<br /> Event Hub     | Azure<br /> Storage Queue | RabbitMQ            | InMemory            |
|-----------------------|-------------------------|---------------------------|---------------------------|---------------------|---------------------|
| SendAsync             | :heavy_check_mark:      | :heavy_check_mark:        | :heavy_check_mark:        | :heavy_check_mark:  | :heavy_check_mark:  |
| ListenAsync           | :heavy_check_mark:      | :heavy_check_mark:        | :heavy_check_mark:        | :heavy_check_mark:  | :heavy_check_mark:  |
| GetMessageCountAsync  | :x:                     | :x:                       | :heavy_check_mark:        | :heavy_check_mark:  | :heavy_check_mark:  |
| KeepAliveAsync        | :heavy_check_mark:      | :heavy_minus_sign: (1.)   | :heavy_check_mark:        | :x:                 | :heavy_minus_sign:  |
| ConfirmAsync          | :heavy_check_mark:      | :heavy_minus_sign: (1.)   | :heavy_check_mark:        | :heavy_check_mark:  | :heavy_minus_sign:  |
| RejectAsync           | :heavy_check_mark:      | :heavy_minus_sign: (1.)   | :heavy_check_mark:        | :heavy_check_mark:  | :heavy_check_mark:  |
| DeadLetterAsync       | :heavy_check_mark:      | :x: (2.)                  | :x: (2.)                  | :x: (2.)            | :heavy_check_mark:  |
| User properties       | :heavy_check_mark:      | :heavy_check_mark:        | :x: (3.)                  | :heavy_check_mark:  | :heavy_check_mark:  |

1) Because Event Hub is stream based and not transactional, these method calls are just ignored.
2) Use `receiver.WithDeadLettering(publisher)` to enable dead letter support.
3) Use `receiver.WithPropertiesInContent()` to enable user properties support (not implemented yet).

:heavy_minus_sign: = Noop/Ignored

### Namotion.Messaging

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.svg)](https://www.nuget.org/packages/Namotion.Messaging/)

Contains common helper methods and base implementations of the abstractions:

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

- When `handleMessages` throws an exception, then the messages are abandoned and later reprocessed until they are moved to the dead letter queue.

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

### Namotion.Messaging.Azure.Storage.Queue

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.Azure.Storage.Queue.svg)](https://www.nuget.org/packages/Namotion.Messaging.Azure.Storage.Queue/)

Implementations: 

- **AzureStorageQueuePublisher**
- **AzureStorageQueueReceiver**

Behavior: 

- When `handleMessages` throws an exception, then the messages are rejected and later reprocessed.

Dependencies: 

- [Microsoft.Azure.Storage.Queue](https://www.nuget.org/packages/Microsoft.Azure.Storage.Queue)

### Namotion.Messaging.RabbitMQ

[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.RabbitMQ.svg)](https://www.nuget.org/packages/Namotion.Messaging.RabbitMQ/)

Implementations: 

- **RabbitMessagePublisher**
- **RabbitMessageReceiver**

Behavior: 

- When `handleMessages` throws an exception, then the messages are rejected and later reprocessed.

Dependencies: 

- [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client)
