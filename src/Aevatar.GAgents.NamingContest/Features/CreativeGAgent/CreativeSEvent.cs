using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace Aevatar.GAgents.NamingContest.CreativeGAgent;

[GenerateSerializer]
public class CreativeSEventBase : GEventBase
{
    
}

[GenerateSerializer]
public class AddHistoryChatSEvent : CreativeSEventBase
{
    [Id(0)] public MicroAIMessage Message { get; set; }  
}

[GenerateSerializer]
public class ClearHistoryChatSEvent : CreativeSEventBase
{  
}

[GenerateSerializer]
public class SetNamingSEvent : CreativeSEventBase
{
    [Id(0)] public string Naming { get; set; }
}

[GenerateSerializer]
public class SetAgentInfoSEvent : CreativeSEventBase
{
    [Id(0)] public  string AgentName { get; set; }
    [Id(1)] public string Description { get; set; }
}