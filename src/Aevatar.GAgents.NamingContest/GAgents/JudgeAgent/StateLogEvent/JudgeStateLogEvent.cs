using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.JudgeAgent;

[GenerateSerializer]
public class JudgeCloneStateLogEvent : StateLogEventBase<JudgeCloneStateLogEvent>
{
    [Id(0)] public Guid JudgeGrainId { get; set; }
}

[GenerateSerializer]
public class JudgeClearAIStateLogEvent : JudgeCloneStateLogEvent
{
}

public class AISetAgentStateLogEvent : JudgeCloneStateLogEvent
{
    [Id(0)] public string AgentName { get; set; }
    [Id(1)] public string AgentResponsibility { get; set; }
    [Id(2)] public Guid CloneJudge { get; set; }
}