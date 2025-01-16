using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.MicroAI.Model;

[GenerateSerializer]
public class MicroAIGAgentState: StateBase
{
    [Id(0)]  public  Guid Id { get; set; }
    [Id(1)]  public  string AgentName { get; set; }
    [Id(2)]  public  string AgentResponsibility{ get; set; }
    [Id(3)] public Queue<MicroAIMessage> RecentMessages = new Queue<MicroAIMessage>();
    [Id(4)] public Guid GroupId { get; set; }

    public void Apply(AISetAgentStateLogEvent aiSetAgentStateLogEvent)
    {
        AgentName = aiSetAgentStateLogEvent.AgentName;
        AgentResponsibility = aiSetAgentStateLogEvent.AgentResponsibility;
    }
    
    public void Apply(AiReceiveStateLogEvent aiReceiveStateLogEvent)
    {
        AddMessage(aiReceiveStateLogEvent.Message);
    }
    
    public void Apply(AiReplyStateLogEvent aiReplyStateLogEvent)
    {
        AddMessage(aiReplyStateLogEvent.Message);
    }

    public void Apply(AiClearStateLogEvent clearStateLogEvent)
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

