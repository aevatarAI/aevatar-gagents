using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.NamingContest.HostGAgent;

[GenerateSerializer]
public class HostSEventBase : GEventBase
{
    
}

[GenerateSerializer]
public class AddHistoryChatSEvent : HostSEventBase
{
    [Id(0)] public MicroAIMessage Message { get; set; }  
}

[GenerateSerializer]
public class SetAgentInfoSEvent : HostSEventBase
{
    [Id(0)] public  string AgentName { get; set; }
    [Id(1)] public string Description { get; set; }
}