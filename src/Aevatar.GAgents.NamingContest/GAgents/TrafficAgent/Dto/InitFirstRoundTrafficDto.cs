using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.NamingContest.TrafficGAgent.Dto;

public class InitFirstRoundTrafficDto:ConfigurationBase
{
    public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    public GrainId HostGroupId { get; set; }
    public int Step { get; set; }
    public Guid MostCharmingId { get; set; }
}