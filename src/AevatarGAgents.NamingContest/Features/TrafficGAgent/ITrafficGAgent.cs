using System;
using System.Threading.Tasks;
using AevatarGAgents.MicroAI.Agent;

namespace AevatarGAgents.NamingContest.TrafficGAgent;

public interface ITrafficGAgent:IMicroAIGAgent
{
    Task AddCreativeAgent(string creativeName, Guid creativeGrainId);
    Task AddJudgeAgent(Guid judgeGrainId);
    
    Task AddHostAgent(Guid judgeGrainId);
}