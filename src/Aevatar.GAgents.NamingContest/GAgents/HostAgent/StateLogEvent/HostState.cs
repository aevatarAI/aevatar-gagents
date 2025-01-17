using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;

namespace AiSmart.GAgent.NamingContest.HostAgent;

public class HostState:StateBase
{
    [Id(1)]  public  string AgentName { get; set; }
    [Id(2)]  public  string AgentResponsibility{ get; set; }
    [Id(3)] public Queue<MicroAIMessage> RecentMessages = new Queue<MicroAIMessage>();
    [Id(4)] public string Naming { get; set; }
    [Id(5)] public Guid GroupId { get; set; }

    public void Apply(AddHistoryChatStateLogEvent @event)
    {
        RecentMessages.Enqueue(@event.Message);
    }

    public void Apply(HostClearAIStateLogEvent @event)
    {
        RecentMessages = new Queue<MicroAIMessage>();
    }

    public void Apply(SetNamingStateLogEvent @event)
    {
        Naming = @event.Naming;
    }

    public void Apply(SetAgentInfoStateLogEvent @event)
    {
        AgentName = @event.AgentName;
        AgentResponsibility = @event.Description;
    }
}