using Aevatar.Core.Abstractions;

namespace AiSmart.GAgent.NamingContest.JudgeAgent.Dto;

public class InitJudgeDto:ConfigurationBase
{
    public string AgentName { get; set; }
    public string AgentResponsibility { get; set; }
    public Guid CloneJudgeId = Guid.Empty;
}