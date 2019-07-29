# Namotion.Messaging

[![Azure DevOps](https://img.shields.io/azure-devops/build/rsuter/Namotion/19/master.svg)](https://rsuter.visualstudio.com/Namotion/_build?definitionId=19)
[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.svg)](https://www.nuget.org/packages/Namotion.Messaging/)

The Namotion.Messaging .NET libraries provide abstractions and implementations for message/event queues and data ingestion services.

This enables the following scenarios: 

- Build **multi-cloud capable applications** by easily change messaging technologies on demand.
- Quickly **try out different messaging technologies** to find the best fit for your applications.
- Implement behavior driven integration **tests which can run in-memory** or against different technologies for debugging and faster execution. 
- Provide **better local development experiences** (e.g. replace Service Bus with the dockerizable RabbitMQ technology locally).

## Core packages

### Namotion.Messaging.Abstractions

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

New extension methods on `IMessagePublisher<T>` and `IMessageReceiver<T>`: 

- **SendJsonAsync(...)**
- **ListenJsonAsync(...)**

## Implementation packages

|                      | ServiceBus              | EventHub                   | RabbitMQ                   | InMemory                   |
|----------------------|-------------------------|----------------------------|----------------------------|----------------------------|
| SendAsync            | Supported               | Supported                  | Supported                  | Supported                  |
| ListenAsync          | Supported               | Supported                  | Supported                  | Supported                  |
| GetMessageCountAsync | Not supported           | Not supported              | Supported                  | Supported                  |
| KeepAliveAsync       | Supported               | Ignored (1.)               | Not supported              | Ignored                    |
| ConfirmAsync         | Supported               | Ignored (1.)               | Supported                  | Ignored                    |
| RejectAsync          | Supported               | Ignored (1.)               | Supported                  | Supported                  |
| DeadLetterAsync      | Supported               | Not supported (2.)         | Not supported (2.)         | Supported                  |

1) Because Event Hub is stream based, these method calls are just ignored.
2) Use `receiver.WithDeadLettering(publisher)` to enable dead letter support.

### Namotion.Messaging

Contains common helper methods and technology independent implementations for the abstractions:

- **InMemoryMessagePublisherReceiver:** In-memory message publisher and receiver for integration tests and dependency free local development environments (i.e. use this implementation when no connection strings are defined).

Extension methods to enhance or modify instances: 

- **WithMessageType\<T>():** Changes the type of the interface from `IMessagePublisher`/`IMessageReceiver` to `IMessagePublisher<T>`/`IMessageReceiver<T>`.
- **WithExceptionHandling(logger):** Adds automatic exception handling (TODO: Needs improvements).
- **WithDeadLettering(messagePublisher):** Adds support for a custom dead letter queue, i.e. a call to `DeadLetterAsync()` will confirm the message and publish it to the specified `messagePublisher`.

### Namotion.Messaging.Azure.ServiceBus

Implementations:

- **ServiceBusMessagePublisher**
- **ServiceBusMessageReceiver**

Dependencies: 

- [Microsoft.Azure.ServiceBus](https://www.nuget.org/packages/Microsoft.Azure.ServiceBus/)

### Namotion.Messaging.Azure.EventHub

Implementations:

- **EventHubMessagePublisher**
- **EventHubMessageReceiver**

Dependencies: 

- [Microsoft.Azure.EventHubs.Processor](https://www.nuget.org/packages/Microsoft.Azure.EventHubs.Processor/)

### Namotion.Messaging.RabbitMQ

Implementations:

- **RabbitMessagePublisher**
- **RabbitMessageReceiver**

Dependencies: 

- [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client)

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
            var receiver = new ServiceBusMessageReceiver("MyConnectionString", "myqueue");
            services.AddSingleton<IMessageReceiver>(receiver);
            services.AddHostedService<MyBackgroundService>();
        })
        .Build();

    await host.RunAsync();
}
```
