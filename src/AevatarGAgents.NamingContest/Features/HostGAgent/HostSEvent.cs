using Aevatar.Core.Abstractions;
using AevatarGAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace AevatarGAgents.NamingContest.HostGAgent;

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