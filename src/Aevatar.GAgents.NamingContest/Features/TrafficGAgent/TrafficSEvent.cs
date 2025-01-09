using System;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.NamingContest.Common;
using Orleans;

namespace Aevatar.GAgents.NamingContest.TrafficGAgent;

[GenerateSerializer]
public class TrafficEventSourcingBase : GEventBase
{
}

[GenerateSerializer]
public class TrafficCallSelectGrainIdSEvent : TrafficEventSourcingBase
{
   [Id(0)] public Guid GrainId { get; set; }
}

[GenerateSerializer]
public class TrafficCreativeFinishSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficGrainCompleteSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid CompleteGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficDebateCompleteGEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid CompleteGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficNameStartSEvent : TrafficEventSourcingBase
{
    [Id(0)] public string Content { get; set; }
}

[GenerateSerializer]
public class TrafficSetAgentSEvent : TrafficEventSourcingBase
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public string Description { get; set; }
}

[GenerateSerializer]
public class AddCreativeAgent : TrafficEventSourcingBase
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
    [Id(1)] public string CreativeName { get; set; }
    [Id(2)] public string Naming { get; set; } 
}

[GenerateSerializer]
public class ChangeNamingStepSEvent : TrafficEventSourcingBase
{
    [Id(0)] public NamingContestStepEnum Step { get; set; }
}

[GenerateSerializer]
public class SetDebateCountSEvent : TrafficEventSourcingBase
{
    [Id(0)] public int DebateCount { get; set; }
}

[GenerateSerializer]
public class ReduceDebateRoundSEvent : TrafficEventSourcingBase
{
}

[GenerateSerializer]
public class AddChatHistorySEvent : TrafficEventSourcingBase
{
    [Id(0)] public MicroAIMessage ChatMessage { get; set; }
}

[GenerateSerializer]
public class AddJudgeSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid JudgeGrainId { get; set; }
}


[GenerateSerializer]
public class AddHostSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid HostGrainId { get; set; }
}

[GenerateSerializer]
public class ClearCalledGrainsSEvent : TrafficEventSourcingBase
{
}

[GenerateSerializer]
public class CreativeNamingSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid CreativeId { get; set; }
    [Id(1)] public string Naming { get; set; }
}

[GenerateSerializer]
public class SetAskingJudgeSEvent : TrafficEventSourcingBase
{
    [Id(0)] public int AskingJudgeCount { get; set; }
}

[GenerateSerializer]
public class AddAskingJudgeSEvent : TrafficEventSourcingBase
{
    [Id(0)] public Guid AskingJudgeGrainId { get; set; }
}

[GenerateSerializer]
public class SetDiscussionSEvent : TrafficEventSourcingBase
{
    [Id(0)] public int DiscussionCount { get; set; }
}

[GenerateSerializer]
public class DiscussionCountReduceSEvent : TrafficEventSourcingBase
{
    
}

[GenerateSerializer]
public class AddScoreJudgeCountSEvent : TrafficEventSourcingBase
{
    
}

[GenerateSerializer]
public class SetRoundNumberSEvent : TrafficEventSourcingBase
{
    [Id(0)] public int RoundCount { get; set; }
}