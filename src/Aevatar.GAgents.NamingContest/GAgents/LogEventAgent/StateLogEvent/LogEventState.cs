using Aevatar.Core.Abstractions;
using Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.GEvent;

namespace Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.StateLogEvent;

public class LogEventState : StateBase
{
    [Id(0)] public string CallBackUrl { get; set; }

    [Id(1)] public string Round { get; set; }

    [Id(2)] public Guid GroupId { get; set; }

    [Id(3)] public String MostCharmingBackUrl { get; set; }
    [Id(4)] public Guid MostCharmingGroupId { get; set; }

    public void Apply(InitLogEventState @event)
    {
        CallBackUrl = @event.CallBackUrl;
        Round = @event.Round;
        GroupId = @event.GroupId;
        MostCharmingBackUrl = @event.MostCharmingBackUrl;
        MostCharmingGroupId = @event.MostCharmingGroupId;
    }
}