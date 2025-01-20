using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.Dto;

public class InitLogEvent:InitializationEventBase
{
    public string CallBackUrl { get; set; }
    public string Round { get; set; }
    public Guid GroupId { get; set; }
    public String MostCharmingBackUrl { get; set; }
    public Guid MostCharmingGroupId { get; set; }
}