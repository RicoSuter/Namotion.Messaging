# Namotion.Messaging

[![Azure DevOps](https://img.shields.io/azure-devops/build/rsuter/Namotion/19/master.svg)](https://rsuter.visualstudio.com/Namotion/_build?definitionId=19)
[![Nuget](https://img.shields.io/nuget/v/Namotion.Messaging.svg)](https://www.nuget.org/packages/Namotion.Messaging/)

This repository provides technology independent C# abstractions for message/event queues and data ingestion services to build multi-cloud capable applications and behavior driven integration testing and improved local development experiences.

## Packages

### Namotion.Messaging.Abstractions

Contains the messaging abstractions, mainly interfaces with a very small footprint and extremely stable contracts:

- IMessagePublisher
- IMessageReceiver

### Namotion.Messaging

Contains common helper methods and technology independent implementations for the abstractions:

- **InMemoryMessagePublisherReceiver:** In-memory message publisher and receiver for integration tests and dependency free local development environments.

### Namotion.Messaging.Azure.ServiceBus

Implementations:

- **ServiceBusMessagePublisher**
- **ServiceBusMessageReceiver**

Dependencies: 

- Microsoft.Azure.ServiceBus

### Namotion.Messaging.Azure.EventHub

Implementations:

- **EventHubMessagePublisher**
- **EventHubMessageReceiver**

Dependencies: 

- Microsoft.Azure.EventHubs.Processor

### Namotion.Messaging.RabbitMQ

Implementations:

- **RabbitMessagePublisher**
- **RabbitMessageReceiver**

Dependencies: 

- RabbitMQ.Client

## Usage

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

public static async Task Main(string[] args)
{
    var host = new HostBuilder()
        .ConfigureServices(services => 
        {
			var receiver = new ServiceBusMessagePublisher("MyConnectionString"], "myqueue");
            services.AddSingleton<IMessageReceiver>(receiver);
            services.AddHostedService<MyBackgroundService>();
        })
        .Build();

    await host.RunAsync();
}
```