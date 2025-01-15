

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.SEvents;

namespace Aevatar.GAgent.TestAgent.NamingContest.CreativeAgent;

[GenerateSerializer]
public class CreativeStateLogEvent : StateLogEventBase <CreativeStateLogEvent>
{
    
}

[GenerateSerializer]
public class AddHistoryChatStateLogEvent : CreativeStateLogEvent
{
    [Id(0)] public MicroAIMessage Message { get; set; }  
}

[GenerateSerializer]
public class ClearHistoryChatStateLogEvent : CreativeStateLogEvent
{  
}

[GenerateSerializer]
public class SetNamingStateLogEvent : CreativeStateLogEvent
{
    [Id(0)] public string Naming { get; set; }
}

[GenerateSerializer]
public class SetAgentInfoStateLogEvent : CreativeStateLogEvent
{
    [Id(0)] public  string AgentName { get; set; }
    [Id(1)] public string Description { get; set; }
}

[GenerateSerializer]
public class SetExecuteStep : CreativeStateLogEvent
{
    [Id(0)] public int Step { get; set; }
}