using Orleans;

namespace AevatarGAgents.MicroAI.Agent.GEvents;
[GenerateSerializer]
public class AISetAgentMessageGEvent:AIMessageGEvent
{
    [Id(0)] public string AgentName { get; set; } 
    [Id(1)] public string AgentResponsibility { get; set; } 
}