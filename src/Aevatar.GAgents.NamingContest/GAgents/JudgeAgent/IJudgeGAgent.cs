
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;

namespace AiSmart.GAgent.NamingContest.JudgeAgent;

public interface IJudgeGAgent:IStateGAgent<JudgeState>
{
    // Task<IJudgeGAgent> Clone();
    // Task SetRealJudgeGrainId(Guid judgeGrainId);
}