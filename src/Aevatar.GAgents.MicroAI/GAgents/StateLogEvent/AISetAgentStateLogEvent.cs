using Orleans;

namespace Aevatar.GAgents.MicroAI.Model;
[GenerateSerializer]
public class AISetAgentStateLogEvent:AIMessageStateLogEvent
{
    [Id(0)] public string AgentName { get; set; } 
    [Id(1)] public string AgentResponsibility { get; set; } 
}