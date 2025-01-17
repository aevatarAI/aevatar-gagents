using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;

namespace AiSmart.GAgent.NamingContest.VoteAgent;

[GenerateSerializer]
public class VoteCharmingStateLogEvent : StateLogEventBase<VoteCharmingStateLogEvent>
{
    [Id(0)] public Dictionary<Guid, string> AgentIdNameDictionary { get; set; } = new();
    [Id(1)] public int Round { get; set; }
    [Id(2)] public List<MicroAIMessage> VoteMessage { get; set; }
}

[GenerateSerializer]
public class InitVoteCharmingStateLogEvent : VoteCharmingStateLogEvent
{
    [Id(0)] public List<Guid> GrainGuidList { get; set; }
    [Id(1)] public int TotalBatches { get; set; }

    [Id(2)] public int Round { get; set; }
    [Id(3)] public Dictionary<Guid, string> GrainGuidTypeDictionary { get; set; }
    [Id(4)] public List<Guid> GroupList { get; set; } = new List<Guid>();
}



[GenerateSerializer]
public class GroupVoteCompleteStateLogEvent : VoteCharmingStateLogEvent
{
    [Id(0)] public List<Guid> VoteGroupList { get; set; }
}