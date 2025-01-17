using Orleans;

namespace Aevatar.GAgents.MicroAI.Model;
[GenerateSerializer]
public class AiReceiveStateLogEvent : AIMessageStateLogEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; }
}