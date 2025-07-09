using System;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

[GenerateSerializer]
public class DirectAIGAgentEvent : StateLogEventBase<DirectAIGAgentEvent>
{
    // Base event for DirectAIGAgent functionality
}

[GenerateSerializer]
public class ChatResponseEvent : DirectAIGAgentEvent
{
    [Id(0)]
    public string Response { get; set; } = "";
    
    [Id(1)]
    public DateTime Timestamp { get; set; }
} 