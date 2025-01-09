using System.Threading.Tasks;

namespace Aevatar.GAgents.NamingContest.TrafficGAgent;

public interface ISecondTrafficGAgent:ITrafficGAgent
{
    Task SetAskJudgeNumber(int judgeNum);
    
    Task SetRoundNumber(int round);
}