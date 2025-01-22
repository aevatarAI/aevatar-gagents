using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.JudgeAgent.Dto;

public class InitJudgeDto:InitializationEventBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
    public Guid CloneJudgeId = Guid.Empty;
}