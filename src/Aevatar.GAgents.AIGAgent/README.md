# Aevatar.GAgents.AIGAgent

## Overview

The Aevatar.GAgents.AIGAgent module is a foundational class library designed for developing AI GAgents. 
This module provides a robust base class that developers can extend to create custom AI GAgents. 
By leveraging this library, developers can focus on the unique logic and behavior of their AI GAgents 
without worrying about the underlying infrastructure.

## Core Components

### IAIGAgent
Defines the interface for AI GAgents.

### GAgentBase
The BaseAIGAgent class is the core component of this module. It provides essential methods and properties 
that every AI GAgent should have, such as initialization, chat, and upload knowledge.

### AIGAgentStateBase
The AIGAgentStateBase class is used to manage the state of the AI GAgent, allowing for state persistence 
and retrieval.

## Usage

### Installation
To use the `Aevatar.GAgents.AIGAgent`, you need to add the NuGet package to your project.

```bash
Install-Package Aevatar.GAgents.AIGAgent
```

### Creating GAgentState
Create your GAgentState by inheriting from AIGAgentStateBase.

```csharp
[GenerateSerializer]
public class MyState : AIGAgentStateBase
{
    // Add custom state here
}
```

### Creating StateLogEvent
Create your StateLogEvent by inheriting from StateLogEventBase.

```csharp
[GenerateSerializer]
public class MyStateLogEvent: StateLogEventBase<MyStateLogEvent>
{
    // Add custom properties
}
```

### Creating IGAgent
Create your IGAgent by inheriting from IAIGAgent and IGAgent.

```csharp
public interface IMyGAgent : IAIGAgent, IGAgent
{
   // Define custom methods here
}
```

### Creating Event
Create your Event by inheriting from EventBase.
```csharp
[GenerateSerializer]
public class MyEvent : EventBase
{
    // Add custom properties
}
```

### Creating GAgent
Create your GAgent by inheriting from AIGAgentBase. You can use the AI functionality by calling the 
ChatWithHistory method from the base class.

```csharp
public class MyGAgent : AIGAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    [EventHandler]
    public async Task HandleEventAsync(MyEvent @event)
    {
        var result = await base.ChatWithHistory(promt);
        // Handle the result as needed
    }
}
```
### Initialize and call GAgent
Before using your GAgent, you need to initialize it by calling the InitializeAsync method to set up 
Instructions and the LLM. The example below uses AzureOpenAI. For more support, please refer to 
the Aevatar.GAgents.SemanticKernel module.

```csharp
var client = host.Services.GetRequiredService<IClusterClient>();
var groupGAgent = client.GetGrain<IGroupGAgent>(Guid.NewGuid());
var myGAgent = client.GetGrain<IMyGAgent>(Guid.NewGuid());
await groupGAgent.RegisterAsync(myGAgent);

await myGAgent.InitializeAsync(new InitializeDto
{
    Instructions = "You are a AI Agent",
    LLM = "AzureOpenAI"
});

await groupGAgent.PublishEventAsync(new MyEvent());
```

You can also upload knowledge using the UploadKnowledge method. 

Two types are currently supported: string and pdf.
```csharp
await myGAgent.UploadKnowledge(new List<BrainContentDto>
{
    new BrainContentDto("pdf knowledge", BrainContentType.Pdf, pdfBytes),
    new BrainContentDto("string knowledge","content")
});
```

## License

Distributed under the MIT License. See [License](../../LICENSE) for more information.