using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.NamingContest.VoteGAgent;

[GenerateSerializer]
public class VoteCharmingState: StateBase
{
    [Id(0)] public List<Guid> VoterIds { get; set; } = new List<Guid>();
    [Id(1)] public int TotalBatches { get; set; }
    [Id(2)] public int CurrentBatch { get; set; }
    [Id(3)] public int Round { get; set; }
    [Id(4)] public Dictionary<Guid, string> VoterIdTypeDictionary { get; set; } = new();

    public void Apply(InitVoteCharmingGEvent @event)
    {
        VoterIds.AddRange(@event.GrainGuidList);
        TotalBatches = @event.TotalBatches;
        Round = @event.Round;
        VoterIdTypeDictionary = @event.GrainGuidTypeDictionary;
    }

    public void Apply(VoteCharmingGEvent @event)
    {
        VoterIds.RemoveAll(@event.GrainGuidList);
        foreach (var voterId in VoterIds)
        {
            VoterIdTypeDictionary.Remove(voterId);
        }
        CurrentBatch++;
    }
   
}

public class RankInfo
{
    [Id(0)] public Guid CreativeGrainId { get; set; }
    [Id(1)] public string Reply { get; set; }
    [Id(2)] public int Score { get; set; }
    [Id(3)] public string CreativeName { get; set; }
}
