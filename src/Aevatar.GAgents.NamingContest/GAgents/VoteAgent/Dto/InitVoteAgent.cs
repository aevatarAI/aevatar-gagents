using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.VoteAgent.Dto;

public class InitVoteAgent : EventBase
{
    [Id(1)] public int TotalBatches { get; set; }
    [Id(2)] public int Round { get; set; }
    [Id(4)] public List<Guid> GroupList { get; set; } = new List<Guid>();
}