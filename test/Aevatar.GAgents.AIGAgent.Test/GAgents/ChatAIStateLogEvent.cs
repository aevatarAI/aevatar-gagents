using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents;

[GenerateSerializer]
public class ChatAIStateLogEvent : StateLogEventBase<ChatAIStateLogEvent>
{
    
}

[GenerateSerializer]
public class ReceiveMessageLogEvent : ChatAIStateLogEvent
{
    [Id(0)] public bool ReceiveMessage { get; set; }
}