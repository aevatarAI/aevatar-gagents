using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.NamingContest.GAgents.LogEventAgent.GEvent;

public class LogEventStateEvent: StateLogEventBase<LogEventStateEvent>
{
    
}

public class InitLogEventState : LogEventStateEvent
{
    public string CallBackUrl { get; set; }
    public string Round { get; set; }
    public Guid GroupId { get; set; }
    public String MostCharmingBackUrl { get; set; }
    public Guid MostCharmingGroupId { get; set; }
}