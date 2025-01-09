using Orleans;

namespace Aevatar.GAgents.MicroAI.Agent.GEvents;
[GenerateSerializer]
public class AIReplyMessageGEvent : AIMessageGEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; } 
 
}