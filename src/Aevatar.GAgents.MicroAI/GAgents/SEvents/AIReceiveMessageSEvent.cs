using Orleans;

namespace Aevatar.GAgents.MicroAI.Agent.GEvents;
[GenerateSerializer]
public class AIReceiveMessageGEvent : AIMessageGEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; }
}