using Aevatar.Core.Abstractions;

namespace Aevatar.GAgent.NamingContest.TrafficGAgent.Dto;

public class InitSecondRoundTrafficDto:ConfigurationBase
{
    public List<CreativeInfo> CreativeList { get; set; } = new List<CreativeInfo>();
    public List<Guid> JudgeAgentList { get; set; } = new List<Guid>();
    public int AskJudgeCount { get; set; }
    public string AgentName { get; set; }
    public string AgentDescription { get; set; }
    public int Round { get; set; }
    public int Step { get; set; }
    public List<Guid> HostAgentList { get; set; } = new List<Guid>();
    public GrainId HostGroupId { get; set; }
    public Guid MostCharmingId { get; set; }
}