using Aevatar.GAgents.MicroAI.Agent.SEvents;
using Orleans;

namespace Aevatar.GAgents.MicroAI.Agent.GEvents;
[GenerateSerializer]
public class AISetAgentMessageSEvent:AIMessageSEvent
{
    [Id(0)] public string AgentName { get; set; } 
    [Id(1)] public string AgentResponsibility { get; set; } 
}