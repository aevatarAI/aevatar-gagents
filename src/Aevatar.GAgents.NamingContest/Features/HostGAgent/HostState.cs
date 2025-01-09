using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.NamingContest.CreativeGAgent;
using Orleans;

namespace Aevatar.GAgents.NamingContest.HostGAgent;

public class HostState:StateBase
{
    [Id(1)]  public  string AgentName { get; set; }
    [Id(2)]  public  string AgentResponsibility{ get; set; }
    [Id(3)] public Queue<MicroAIMessage> RecentMessages = new Queue<MicroAIMessage>();
    [Id(4)] public string Naming { get; set; }
    [Id(5)] public Guid GroupId { get; set; }

    public void Apply(AddHistoryChatSEvent @event)
    {
        RecentMessages.Enqueue(@event.Message);
    }

    public void Apply(ClearHistoryChatSEvent @event)
    {
        RecentMessages.Clear();
    }

    public void Apply(SetNamingSEvent @event)
    {
        Naming = @event.Naming;
    }

    public void Apply(SetAgentInfoSEvent @event)
    {
        AgentName = @event.AgentName;
        AgentResponsibility = @event.Description;
    }
}