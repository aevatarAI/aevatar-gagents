using System.Threading.Tasks;
using AevatarGAgents.MicroAI.Agent;

namespace AevatarGAgents.NamingContest.CreativeGAgent;

public interface ICreativeGAgent:IMicroAIGAgent
{
    // Task InitAgentsAsync(ContestantAgent contestantAgent);
    Task<string> GetCreativeNaming();

    Task<string> GetCreativeName();

}