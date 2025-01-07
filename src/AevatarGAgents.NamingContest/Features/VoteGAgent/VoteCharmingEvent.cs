using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using AevatarGAgents.MicroAI.Agent.GEvents;
using Orleans;

namespace AevatarGAgents.NamingContest.VoteGAgent;

[GenerateSerializer]
public class VoteCharmingEvent:EventBase
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
}

[GenerateSerializer]
public class InitVoteCharmingEvent:EventBase
{
    [Id(0)] public int TotalBatches { get; set; }
    [Id(1)] public int Round { get; set; }
    [Id(2)] public List<Guid> JudgeGuidList { get; set; } = new();
    [Id(3)] public List<Guid> CreativeGuidList { get; set; } = new();


}

[GenerateSerializer]
public class VoteCharmingCompleteEvent:EventBase
{
    [Id(0)] public Guid Winner { get; set; }
    [Id(1)] public Guid VoterId { get; set; }
    [Id(2)] public int Round { get; set; }
}
