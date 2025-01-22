
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;

namespace AiSmart.GAgent.NamingContest.HostAgent;

[GenerateSerializer]
public class HostStateLogEvent :  StateLogEventBase<HostStateLogEvent>
{
    
}

[GenerateSerializer]
public class AddHistoryChatStateLogEvent : HostStateLogEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; }  
}

[GenerateSerializer]
public class SetAgentInfoStateLogEvent : HostStateLogEvent
{
    [Id(0)] public  string AgentName { get; set; }
    [Id(1)] public string Description { get; set; }
}

[GenerateSerializer]
public class HostClearAIStateLogEvent : HostStateLogEvent
{
}

[GenerateSerializer]
public class SetNamingStateLogEvent : HostStateLogEvent
{
    [Id(0)] public string Naming { get; set; }
}