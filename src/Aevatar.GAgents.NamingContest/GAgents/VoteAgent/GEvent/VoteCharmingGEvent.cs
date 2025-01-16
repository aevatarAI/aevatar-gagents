using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;

namespace AiSmart.GAgent.NamingContest.VoteAgent;

[GenerateSerializer]
public class VoteCharmingGEvent:EventBase
{
    [Id(0)] public Dictionary<Guid, string> AgentIdNameDictionary { get; set; } = new();
    [Id(1)] public int Round { get; set; }
    [Id(2)] public List<MicroAIMessage> VoteMessage { get; set; } 
}

[GenerateSerializer]
public class SingleVoteCharmingEvent:EventBase
{
    [Id(0)] public Dictionary<Guid, string> AgentIdNameDictionary { get; set; } = new();
    [Id(1)] public List<MicroAIMessage> VoteMessage { get; set; } = new();
    [Id(2)] public int Round { get; set; }
    [Id(3)] public Guid VoteCharmingGrainId { get; set; }
}

[GenerateSerializer]
public class InitVoteCharmingEvent:EventBase
{
    [Id(0)] public int TotalBatches { get; set; }
    [Id(1)] public int Round { get; set; }
    [Id(2)] public List<Guid> JudgeGuidList { get; set; } = new();
    [Id(3)] public List<Guid> CreativeGuidList { get; set; } = new();
    [Id(4)] public List<Guid> groupList { get; set; } = new List<Guid>();   
}

[GenerateSerializer]
public class VoteCharmingCompleteEvent:EventBase
{
    [Id(0)] public Guid Winner { get; set; }
    [Id(1)] public Guid VoterId { get; set; }
    [Id(2)] public int Round { get; set; }
}
