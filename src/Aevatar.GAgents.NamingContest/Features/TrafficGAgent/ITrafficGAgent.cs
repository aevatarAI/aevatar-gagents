using System;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent;

namespace Aevatar.GAgents.NamingContest.TrafficGAgent;

public interface ITrafficGAgent:IMicroAIGAgent
{
    Task AddCreativeAgent(string creativeName, Guid creativeGrainId);
    Task AddJudgeAgent(Guid judgeGrainId);
    
    Task AddHostAgent(Guid judgeGrainId);
}