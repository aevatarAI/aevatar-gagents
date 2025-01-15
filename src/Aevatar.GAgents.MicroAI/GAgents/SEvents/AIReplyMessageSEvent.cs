using Aevatar.GAgents.MicroAI.Agent.SEvents;
using Orleans;

namespace Aevatar.GAgents.MicroAI.Agent.GEvents;
[GenerateSerializer]
public class AiReplyMessageSEvent : AIMessageSEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; } 
 
}