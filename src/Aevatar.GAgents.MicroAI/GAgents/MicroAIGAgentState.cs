using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Agent.SEvents;
using Orleans;

namespace Aevatar.GAgents.MicroAI.GAgent;

[GenerateSerializer]
public class MicroAIGAgentState: StateBase
{
    [Id(0)]  public  Guid Id { get; set; }
    [Id(1)]  public  string AgentName { get; set; }
    [Id(2)]  public  string AgentResponsibility{ get; set; }
    [Id(3)] public Queue<MicroAIMessage> RecentMessages = new Queue<MicroAIMessage>();
    [Id(4)] public Guid GroupId { get; set; }

    public void Apply(AISetAgentMessageSEvent aiSetAgentMessageSEvent)
    {
        AgentName = aiSetAgentMessageSEvent.AgentName;
        AgentResponsibility = aiSetAgentMessageSEvent.AgentResponsibility;
    }
    
    public void Apply(AiReceiveMessageSEvent aiReceiveMessageSEvent)
    {
        AddMessage(aiReceiveMessageSEvent.Message);
    }
    
    public void Apply(AiReplyMessageSEvent aiReplyMessageSEvent)
    {
        AddMessage(aiReplyMessageSEvent.Message);
    }

    public void Apply(AIClearMessageSEvent clearMessageSEvent)
    {
        RecentMessages = new Queue<MicroAIMessage>();
    }
    
    void AddMessage(MicroAIMessage message)
    {
        if (RecentMessages.Count == 10)
        {
            RecentMessages.Dequeue(); 
        }
        RecentMessages.Enqueue(message); 
    }

}

