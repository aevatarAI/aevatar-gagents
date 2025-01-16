
using Aevatar.GAgents.MicroAI.GAgent;

namespace AiSmart.GAgent.NamingContest.JudgeAgent;

public interface IJudgeGAgent:IMicroAIGAgent
{
    Task<IJudgeGAgent> Clone();

    Task SetRealJudgeGrainId(Guid judgeGrainId);
}