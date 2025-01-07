using System.Threading.Tasks;

namespace AevatarGAgents.NamingContest.TrafficGAgent;

public interface ISecondTrafficGAgent:ITrafficGAgent
{
    Task SetAskJudgeNumber(int judgeNum);
    
    Task SetRoundNumber(int round);
}