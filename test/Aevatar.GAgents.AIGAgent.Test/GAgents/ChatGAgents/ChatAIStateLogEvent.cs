using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.GAgents.ChatGAgents;

[GenerateSerializer]
public class ChatAIStateLogEvent : StateLogEventBase<ChatAIStateLogEvent>
{
    
}

[GenerateSerializer]
public class AddMessageLogEvent : ChatAIStateLogEvent
{
    [Id(0)] public AIStreamChatContent Content { get; set; }
}
