using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.StateLogEvent;

public class LogEventState:StateBase
{
    [Id(0)] public string CallBackUrl { get; set; }

    [Id(1)] public string Round { get; set; }
    
    [Id(2)] public Guid groupId { get; set; }

    [Id(3)] public String MostCharmingBackUrl { get; set; }
    [Id(4)] public Guid MostCharmingGroupId { get; set; }
}