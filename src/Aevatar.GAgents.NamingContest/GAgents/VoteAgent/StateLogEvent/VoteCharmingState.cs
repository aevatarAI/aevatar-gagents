using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.VoteAgent;

[GenerateSerializer]
public class VoteCharmingState : StateBase
{
    [Id(0)] public List<Guid> VoterIds { get; set; } = new List<Guid>();
    [Id(1)] public int TotalBatches { get; set; }
    [Id(2)] public int CurrentBatch { get; set; }
    [Id(3)] public int Round { get; set; }
    [Id(4)] public Dictionary<Guid, string> VoterIdTypeDictionary { get; set; } = new();
    [Id(5)] public List<Guid> GroupList { get; set; } = new List<Guid>();
    [Id(6)] public int TotalGroupCount { get; set; } = 0;
    [Id(7)] public int GroupHasVoteCount { get; set; } = 0;

    public void Apply(InitVoteCharmingStateLogEvent @event)
    {
        VoterIds.AddRange(@event.GrainGuidList);
        TotalBatches = @event.TotalBatches;
        Round = @event.Round;
        VoterIdTypeDictionary = @event.GrainGuidTypeDictionary;
        var group = GroupList.Slice(0, GroupList.Count);
        var addGroup = 0;
        foreach (var item in @event.GroupList.Where(item => group.Contains(item) == false))
        {
            group.Add(item);
            addGroup += 1;
        }

        GroupList = group;
        TotalGroupCount += addGroup;
    }
    
    public void Apply(GroupVoteCompleteStateLogEvent @event)
    {
        if (@event.VoteGroupList.Count > 0)
        {
            var list = GroupList.Slice(0, GroupList.Count);
            foreach (var t in @event.VoteGroupList)
            {
                list.RemoveAll(f => f == t);
            }

            GroupList = list;
        }

        if (GroupList.Count == 0)
        {
            GroupHasVoteCount = 0;
            TotalGroupCount = 0;
        }
        else
        {
            GroupHasVoteCount += 1;
        }
    }
}