# Namotion.Messaging

[![Azure DevOps](https://img.shields.io/azure-devops/build/rsuter/Namotion/19/master.svg)](https://rsuter.visualstudio.com/Namotion/_build?definitionId=19)
[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.svg)](https://www.nuget.org/packages/Namotion.Messaging/)

The Namotion.Messaging .NET libraries provide abstractions and implementations for message/event queues and data ingestion services.

This enables the following scenarios: 

- Build **multi-cloud capable applications** by easily change messaging technologies on demand.
- Quickly **try out different messaging technologies** to find the best fit for your applications.
- Implement behavior driven integration **tests which can run in-memory** or against different technologies for debugging and faster exection. 
- Provide **better local development experiences** (e.g. replace Service Bus with the dockerizable RabbitMQ technology locally).

## Packages

### Namotion.Messaging.Abstractions

Contains the messaging abstractions, mainly interfaces with a very small footprint and extremely stable contracts:

- **IMessagePublisher**
- **IMessageReceiver**
- **Message:** A generic message implementation.

### Namotion.Messaging

Contains common helper methods and technology independent implementations for the abstractions:

- **InMemoryMessagePublisherReceiver:** In-memory message publisher and receiver for integration tests and dependency free local development environments (i.e. use this implementation when no connection strings are defined).

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

To use the `IMessageReceiver` in a simple command line application, implement the a new `BackgroundService` and start message processing in `ExecuteAsync`:

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
            var receiver = new ServiceBusMessagePublisher("MyConnectionString", "myqueue");
            services.AddSingleton<IMessageReceiver>(receiver);
            services.AddHostedService<MyBackgroundService>();
        })
        .Build();

    await host.RunAsync();
}
```