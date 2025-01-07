using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.NamingContest.VoteGAgent;

[GenerateSerializer]
public class VoteCharmingGEvent:GEventBase
{
    [Id(0)] public List<Guid> GrainGuidList { get; set; }
}

[GenerateSerializer]
public class InitVoteCharmingGEvent:GEventBase
{
    [Id(0)] public List<Guid> GrainGuidList { get; set; }
    [Id(1)] public int TotalBatches { get; set; }
    
    [Id(2)] public int Round { get; set; }
    [Id(3)] public Dictionary<Guid, string> GrainGuidTypeDictionary { get; set; }
}
