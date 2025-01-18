

using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;

namespace Aevatar.GAgent.NamingContest.TrafficGAgent;

[GenerateSerializer]
public class TrafficEventStateLogEvent : StateLogEventBase<TrafficEventStateLogEvent>
{
}

[GenerateSerializer]
public class TrafficCallSelectGrainIdSEvent : TrafficEventStateLogEvent
{
   [Id(0)] public Guid GrainId { get; set; }
}

[GenerateSerializer]
public class TrafficCreativeFinishSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficGrainCompleteSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid CompleteGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficDebateCompleteGEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid CompleteGrainId { get; set; }
}

[GenerateSerializer]
public class TrafficNameStartSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public string Content { get; set; }
}

[GenerateSerializer]
public class FirstTrafficSetAgentSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    [Id(1)] public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    [Id(2)] public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    [Id(3)] public Guid HostGroupId { get; set; }
    [Id(4)] public int Step { get; set; }
    [Id(5)] public Guid MostCharmingId { get; set; }
}

[GenerateSerializer]
public class SecondTrafficSetAgentSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    [Id(1)] public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    [Id(2)] public int AskJudgeCount { get; set; }
    [Id(3)] public string AgentName { get; set; }
    [Id(4)] public string AgentDescription { get; set; }
    [Id(5)] public int Round { get; set; }
    [Id(6)] public int Step { get; set; }
    [Id(7)] public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    [Id(8)] public Guid HostGroupId { get; set; }
    [Id(9)] public Guid MostCharmingId { get; set; }
}

[GenerateSerializer]
public class AddCreativeAgent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
    [Id(1)] public string CreativeName { get; set; }
    [Id(2)] public string Naming { get; set; } 
}

[GenerateSerializer]
public class ChangeNamingStepSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public NamingContestStepEnum Step { get; set; }
}

[GenerateSerializer]
public class SetDebateCountSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public int DebateCount { get; set; }
}

[GenerateSerializer]
public class ReduceDebateRoundSEvent : TrafficEventStateLogEvent
{
}

[GenerateSerializer]
public class AddChatHistorySEvent : TrafficEventStateLogEvent
{
    [Id(0)] public MicroAIMessage ChatMessage { get; set; }
}

[GenerateSerializer]
public class AddJudgeSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid JudgeGrainId { get; set; }
}


[GenerateSerializer]
public class AddHostSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid HostGrainId { get; set; }
}

[GenerateSerializer]
public class ClearCalledGrainsSEvent : TrafficEventStateLogEvent
{
}

[GenerateSerializer]
public class CreativeNamingSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid CreativeId { get; set; }
    [Id(1)] public string Naming { get; set; }
}

[GenerateSerializer]
public class SetAskingJudgeSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public int AskingJudgeCount { get; set; }
}

[GenerateSerializer]
public class AddAskingJudgeSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public Guid AskingJudgeGrainId { get; set; }
}

[GenerateSerializer]
public class SetDiscussionSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public int DiscussionCount { get; set; }
}

[GenerateSerializer]
public class DiscussionCountReduceSEvent : TrafficEventStateLogEvent
{
    
}

[GenerateSerializer]
public class AddScoreJudgeCountSEvent : TrafficEventStateLogEvent
{
    
}

[GenerateSerializer]
public class SetRoundNumberSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public int RoundCount { get; set; }
}

[GenerateSerializer]
public class SetStepNumberSEvent : TrafficEventStateLogEvent
{
    [Id(0)] public int StepCount { get; set; }
}
