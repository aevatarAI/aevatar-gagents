
using AiSmart.GAgent.TestAgent.NamingContest.TrafficAgent;

namespace AiSmart.GAgent.NamingContest.TrafficAgent;

public interface ISecondTrafficGAgent:ITrafficGAgent
{
    Task SetAskJudgeNumber(int judgeNum);
    
    Task SetRoundNumber(int round);
}