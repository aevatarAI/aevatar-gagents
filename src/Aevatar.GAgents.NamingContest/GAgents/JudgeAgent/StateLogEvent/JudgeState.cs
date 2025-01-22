using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Model;


namespace AiSmart.GAgent.NamingContest.JudgeAgent;

[GenerateSerializer]
public class JudgeState : StateBase
{
    [Id(1)] public string AgentName { get; set; }
    [Id(2)] public string AgentResponsibility { get; set; }
    [Id(4)] public Guid CloneJudgeId = Guid.Empty;
    [Id(3)] public Queue<MicroAIMessage> RecentMessages = new Queue<MicroAIMessage>();
    
    public void Apply(AISetAgentStateLogEvent aiSetAgentStateLogEvent)
    {
        AgentName = aiSetAgentStateLogEvent.AgentName;
        AgentResponsibility = aiSetAgentStateLogEvent.AgentResponsibility;
        CloneJudgeId = aiSetAgentStateLogEvent.CloneJudge;
    }
    
    public void Apply(AiReceiveStateLogEvent aiReceiveStateLogEvent)
    {
        AddMessage(aiReceiveStateLogEvent.Message);
    }
    
    public void Apply(AiReplyStateLogEvent aiReplyStateLogEvent)
    {
        AddMessage(aiReplyStateLogEvent.Message);
    }

    public void Apply(JudgeClearAIStateLogEvent judgeClearAiStateLogEvent)
    {
        RecentMessages = new Queue<MicroAIMessage>();
    }

    public void Apply(JudgeCloneStateLogEvent @event)
    {
        CloneJudgeId = @event.JudgeGrainId;
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